using SyncEd.Network.Packets;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	internal class PeerObject
	{
		internal Peer Peer { get; set; }
		internal object Object { get; set; }

		public override string ToString()
		{
			return "PeerObject {" + Peer + ", " + Object + "}";
		}
	}

	[Serializable]
	internal class UdpPacket
	{
		internal string DocumentName { get; set; }
	}

	[Serializable]
	internal class FindPacket : UdpPacket
	{
		internal int ListenPort { get; set; }
	}

	[Serializable]
	internal class PeerDiedPacket : UdpPacket
	{
		internal Peer DeadPeer { get; set; }
		internal Peer RepairPeer { get; set; }
	}

	internal class PeerComparer : IComparer<Peer>
	{
		private int CompareBytes(byte[] a, byte[] b)
		{
			for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
			{
				int r = a[i] - b[i];
				if (r != 0)
					return r;
				continue;
			}

			return a.Length - b.Length;
		}

		public int Compare(Peer a, Peer b)
		{
			int r = CompareBytes(a.EndPoint.Address.GetAddressBytes(), b.EndPoint.Address.GetAddressBytes());
			if (r != 0)
				return r;
			return a.EndPoint.Port.CompareTo(b.EndPoint.Port);
		}
	}

	public class TcpNetwork : INetwork
	{
		public event PacketHandler PacketArrived;

		private List<TcpLink> links;
		private string documentName;

		private List<Tuple<PeerObject, TcpLink>> repairModeOutgoingTcpPacketBuffer;
		private SortedSet<Peer> repairMasterPeers;
		private Peer repairDeadPeer;

		private bool InRepairMode { get { return repairDeadPeer != null; } }

		private const int repairMasterNodeWaitMs = 200;
		private const int repairReestablishWaitMs = 1000;

		private const int broadcastPort = 1337; // UDP port for sending broadcasts
		private const int linkEstablishTimeoutMs = 200;
		private const int linkHandshakeTimeoutMs = 200;

		private int tcpListenPort = 1338;    // first tried TCP port for listening after broadcasts

		private UdpClient udp;
		private Thread udpListenThread;
		private TcpListener tcpListener;

		private ManualResetEvent ownIPWaitHandle;

		private BinaryFormatter formatter = new BinaryFormatter();

		#region public interface
		public Peer Self { get; private set; }

		/// <summary>
		/// Starts the link control system which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		public bool Start(string documentName)
		{
			this.documentName = documentName;
			ownIPWaitHandle = new ManualResetEvent(false);
			repairModeOutgoingTcpPacketBuffer = new List<Tuple<PeerObject, TcpLink>>();
			repairMasterPeers = new SortedSet<Peer>(new PeerComparer());
			links = new List<TcpLink>();

			tcpListener = FindTcpListener();

			StartListeningForPeers();
			bool found = FindPeer();

			ownIPWaitHandle.WaitOne(); // wait for listener thread to receive self broadcast and determine own IP

			return found;
		}

		public void Stop()
		{
			tcpListener.Stop();
			udp.Close();
			udpListenThread.Join();
			lock (links)
				links.ForEach(p => p.Dispose());
			links = new List<TcpLink>();
			ownIPWaitHandle.Dispose();
		}

		public void SendPacket(object packet)
		{
			Debug.Assert(Self != null, "Own IP has not been determined for send");
			TcpBroadcastObject(new PeerObject() { Peer = Self, Object = packet });
		}
		#endregion

		internal byte[] Serialize(object o)
		{
			using (var ms = new MemoryStream())
			{
				// serialize packet
				formatter.Serialize(ms, o);

				// shrink buffer
				byte[] bytes = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(bytes, 0, (int)ms.Length);

				return bytes;
			}
		}

		internal object Deserialize(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
				return formatter.Deserialize(ms);
		}

		private void OwnIPDetected(IPEndPoint address)
		{
			Self = new Peer() { EndPoint = address };
			Console.WriteLine("Own IP determined as " + address);
			ownIPWaitHandle.Set();
		}

		private void NewLinkEstablished(TcpClient tcp, Peer peer)
		{
			lock (links)
			{
				var l = new TcpLink(tcp, peer, TcpObjectReveived, PeerFailed);
				Debug.Assert(links.Find(t => t.Peer.Equals(peer)) == null);
				links.Add(l);
			}
		}

		private void PeerFailed(TcpLink link, byte[] failedData)
		{
			Console.WriteLine("PANIC - " + link + " is dead");
			RepairDeadLink(link, Self);

			// inform peers that a link died
			UdpBroadcastObject(new PeerDiedPacket() { DocumentName = documentName, DeadPeer = link.Peer, RepairPeer = Self });
		}

		private void UdpBroadcastObject(object o)
		{
			Console.WriteLine("UDP out: " + o.GetType().Name);
			udp.Client.SendTo(Serialize(o), new IPEndPoint(IPAddress.Broadcast, broadcastPort));
		}

		private void TcpBroadcastObject(PeerObject po, TcpLink exclude = null, bool overrideRepair = false)
		{
			if (!overrideRepair && InRepairMode)
			{
				Console.WriteLine("Buffered: " + po.Object);
				repairModeOutgoingTcpPacketBuffer.Add(Tuple.Create(po, exclude));
			}
			else
			{
				Console.WriteLine("TCP out (" + links.Count + "): " + po.Object.GetType().Name);
				byte[] data = Serialize(po);
				lock (links)
					foreach (TcpLink l in links)
						if (l != exclude)
							l.Send(data);
			}
		}

		private void TcpObjectReveived(TcpLink link, object o)
		{
			var po = o as PeerObject;
			Console.WriteLine("TCP in (" + po.Peer.EndPoint + "): " + po.Object.GetType().Name);

			// forward
			if (po.Object.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				TcpBroadcastObject(po, link);

			FirePacketArrived(po.Object, po.Peer, p =>
			{
				Console.WriteLine("TCP out (" + link + "): " + p.GetType().Name);
				link.Send(Serialize(new PeerObject() { Peer = Self, Object = p }));
			});
		}

		private void FirePacketArrived(object packet, Peer peer, SendBackFunc sendBack)
		{
			var handler = PacketArrived;
			if (handler != null)
				handler(packet, peer, sendBack);
		}

		/// <summary>
		/// Returns true if a peer has successfully connected (handshake ok and no timeout)
		/// </summary>
		/// <returns></returns>
		internal bool WaitForTcpConnect()
		{
			//Console.WriteLine("Waiting for TCP connect");
			var peerTask = tcpListener.AcceptTcpClientAsync();
			if (peerTask.Wait(linkEstablishTimeoutMs))
			{
				var tcp = peerTask.Result;

				// receive peer's port
				byte[] portBytes = new byte[sizeof(int)];
				Debug.Assert(tcp.GetStream().Read(portBytes, 0, sizeof(int)) == sizeof(int)); // assume an int gets sent at once
				int remotePort = BitConverter.ToInt32(portBytes, 0);

				var address = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address;
				var peerEp = new IPEndPoint(address, remotePort);
				var peer = new Peer() { EndPoint = peerEp };

				lock (links)
					if (links.Find(l => l.Peer.Equals(peer)) != null)
					{
						Console.WriteLine("Warning: Peer " + peer + " connected twice.");
						tcp.Close();
						return false;
					}

				// send own port
				tcp.GetStream().Write(BitConverter.GetBytes(tcpListenPort), 0, sizeof(int));

				Console.WriteLine("TCP connect from " + peerEp + ". ESTABLISHED");
				//Console.WriteLine("Connection established");
				NewLinkEstablished(tcp, peer);
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		internal bool FindPeer()
		{
			// send a broadcast with the document name into the network
			Console.WriteLine("Broadcasting for " + documentName);
			UdpBroadcastObject(new FindPacket() { DocumentName = documentName, ListenPort = tcpListenPort });

			// wait for an answer

			var r = WaitForTcpConnect();
			if (!r)
				Console.WriteLine("No answer. I'm first owner");
			return r;
		}

		private bool IsLocalAddress(IPAddress address)
		{
			return Dns.GetHostAddresses(Dns.GetHostName()).Any(a => a.Equals(address));
		}

		private TcpListener FindTcpListener()
		{
			// find and open TCP listening port for incoming connection
			TcpListener l = null;
			for (; tcpListenPort < 0xFFFF; tcpListenPort++)
			{
				try
				{
					l = new TcpListener(IPAddress.Any, tcpListenPort);
					l.ExclusiveAddressUse = true;
					l.Start();
					break;
				}
				catch (Exception)
				{
					continue;
				}
			}
			if (l == null)
				throw new Exception("Failed to find a TCP port for listening");

			Console.WriteLine("Bound TCP listener to port " + tcpListenPort);
			return l;
		}

		/// <summary>
		/// Listens on the network for new peers with the given document name.
		/// </summary>
		/// <param name="documentName"></param>
		private void StartListeningForPeers()
		{
			udp = new UdpClient();
			udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			udp.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
			udp.EnableBroadcast = true;

			udpListenThread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						//Console.WriteLine("Waiting for broadcast");
						var ep = new IPEndPoint(IPAddress.Any, broadcastPort);
						byte[] bytes;
						try
						{
							bytes = udp.Receive(ref ep);
						}
						catch (Exception)
						{
							// asume the socket has been closed for shutting down
							return;
						}
						if (bytes != null && bytes.Length != 0)
						{
							var packet = (UdpPacket)Deserialize(bytes);
							//Console.WriteLine("Received broadcast from {0}: {1}", ep.Address, packet.DocumentName);
							ProcessUdpPacket(packet, ep);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Exception in UDP broadcast listening: " + e.ToString());
					}
				}
			});

			udpListenThread.Start();
		}

		private void EstablishConnectionTo(IPEndPoint peerEP)
		{
			lock (links)
				if (links.Find(l => l.Peer.Equals(peerEP)) != null)
				{
					Console.WriteLine("Tried to connet to peer twice.");
					return;
				}

			var tcp = new TcpClient();
			Console.WriteLine("TCP connect to " + peerEP + ": ");
			try
			{
				tcp.Connect(peerEP);
				tcp.ReceiveTimeout = linkHandshakeTimeoutMs;

				// send own port
				tcp.GetStream().Write(BitConverter.GetBytes(tcpListenPort), 0, sizeof(int));

				// receive remote port
				byte[] bytes = new byte[sizeof(int)];
				Debug.Assert(tcp.GetStream().Read(bytes, 0, sizeof(int)) == sizeof(int));
				int remotePort = BitConverter.ToInt32(bytes, 0);
				if (peerEP.Port != remotePort)
					throw new Exception("Port mismatch during handshake");

				tcp.ReceiveTimeout = 0;
			}
			catch (Exception)
			{
				Console.WriteLine("Connect failed");
				tcp.Close();
				return;
			}

			Console.WriteLine("ESTABLISHED");
			NewLinkEstablished(tcp, new Peer() { EndPoint = peerEP });
		}

		private void ProcessUdpPacket(UdpPacket packet, IPEndPoint endpoint)
		{
			Console.WriteLine("UDP in: " + packet.GetType().Name);
			if (packet.DocumentName == documentName)
			{
				if (packet is FindPacket)
					ProcessUdpFind(packet as FindPacket, endpoint);
				else if (packet is PeerDiedPacket)
					ProcessUdpPeerDied(packet as PeerDiedPacket);
				else
					Console.WriteLine("Warning: Unrecognized Udp Packet");
			}
			else
				Console.WriteLine("Document mismatch");
		}

		private void ProcessUdpFind(FindPacket p, IPEndPoint endpoint)
		{
			if (IsLocalAddress(endpoint.Address) && p.ListenPort == tcpListenPort)
				OwnIPDetected(new IPEndPoint(endpoint.Address, tcpListenPort));
			else
				EstablishConnectionTo(new IPEndPoint(endpoint.Address, p.ListenPort));
		}

		private void ProcessUdpPeerDied(PeerDiedPacket p)
		{
			if (InRepairMode)
			{
				if (!p.DeadPeer.Equals(repairDeadPeer))
					Console.WriteLine("FATAL: Incoming panic while currently repairing other node. This is not implemented =/");
				else
					lock (repairMasterPeers)
						repairMasterPeers.Add(p.RepairPeer);
			}
			else
			{
				// check all links if they are affected and kill affected ones
				TcpLink deadLink = null;
				lock (links)
					deadLink = links.Where(l => l.Peer.Equals(p.DeadPeer)).FirstOrDefault();
				Console.WriteLine("All links ok: " + (deadLink == null));
				if (deadLink != null)
					RepairDeadLink(deadLink, p.RepairPeer);
			}
		}

		private void RepairDeadLink(TcpLink deadLink, Peer repairMasterPeer)
		{
			lock (links)
				links.Remove(deadLink);
			deadLink.Dispose();

			Console.WriteLine("Preparing repair mode");
			lock (repairMasterPeers)
			{
				repairMasterPeers.Add(repairMasterPeer);

				// prevent starting repair mode multiple times
				if (!InRepairMode)
				{
					repairDeadPeer = deadLink.Peer;

					// wait a little as some more master node requests might come in
					Task.Delay(repairMasterNodeWaitMs).ContinueWith(t => Repair());
				}
			}
		}

		private void Repair()
		{
			Console.WriteLine("Repair started. Masters:");
			lock (repairMasterPeers)
				foreach (var m in repairMasterPeers)
					Console.WriteLine(m);

			// if we are not the master node, connect to it
			Peer masterNode = null;
			lock (repairMasterPeers)
				masterNode = repairMasterPeers.First();

			Console.WriteLine("Chosen?: " + (masterNode == Self));

			if (masterNode != Self)
			{
				Console.WriteLine("Connecting to repair master");
				EstablishConnectionTo(masterNode.EndPoint);
			}
			else
			{
				Console.WriteLine("Waiting for incoming connections.");
				var sw = Stopwatch.StartNew();
				while (sw.ElapsedMilliseconds < repairReestablishWaitMs)
					WaitForTcpConnect();
				sw.Stop();
			}

			// flush all packets buffered during repair
			Console.WriteLine("Flushing " + repairModeOutgoingTcpPacketBuffer.Count + " packets");
			foreach (var poAndExclude in repairModeOutgoingTcpPacketBuffer)
				TcpBroadcastObject(poAndExclude.Item1, poAndExclude.Item2, true);
			repairModeOutgoingTcpPacketBuffer.Clear();

			// notify the network
			Console.WriteLine("Send peer lost notification");
			if (masterNode == Self)
			{
				var lostPeerPacket = new LostPeerPacket() { };
				FirePacketArrived(lostPeerPacket, Self, p => { });
				TcpBroadcastObject(new PeerObject() { Peer = Self, Object = lostPeerPacket }, null, true);
			}

			// disable repair mode
			repairMasterPeers = new SortedSet<Peer>(new PeerComparer());
			repairDeadPeer = null;

			Console.WriteLine("Repair finished");
		}
	}
}
