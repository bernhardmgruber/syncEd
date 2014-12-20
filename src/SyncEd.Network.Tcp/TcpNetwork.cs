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
		internal Peer Peer { get; set; }
	}

	public class TcpNetwork : INetwork
	{
		public event PacketHandler PacketArrived;

		private List<TcpLink> links;
		private ManualResetEvent ownIPWaitHandle;
		private ManualResetEvent repairModeWaitHandle;
		private string documentName;

		private const int broadcastPort = 1337; // UDP port for sending broadcasts
		private const int linkEstablishTimeoutMs = 1000;

		private int tcpListenPort = 1338;    // first tried TCP port for listening after broadcasts

		private UdpClient udp;
		private Thread udpListenThread;
		private TcpListener tcpListener;

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
			repairModeWaitHandle = new ManualResetEvent(true);
			links = new List<TcpLink>();
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
				links.ForEach(p => p.Close());
			links = new List<TcpLink>();
			ownIPWaitHandle.Dispose();
			repairModeWaitHandle.Dispose();
		}

		public void SendPacket(object packet)
		{
			Debug.Assert(Self != null, "Own IP has not been determined for send");

			byte[] data = Serialize(new PeerObject() { Peer = Self, Object = packet });
			Console.WriteLine("TcpLinkControl: Outgoing (" + links.Count + "): " + packet);
			BroadcastBytes(data);
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

		private void NewLinkEstablished(TcpLink p)
		{
			lock (links)
				links.Add(p);
			p.ObjectReceived += ObjectReveived;
			p.Failed += PeerFailed;
			p.Start();
		}

		private void PeerFailed(TcpLink link, byte[] failedData)
		{
			// remove link
			lock (links)
				links.Remove(link);
			link.Close();

			Console.WriteLine("PANIC - " + link + " is dead");

			repairModeWaitHandle.Reset(); // start repair mode

			// inform peers that a link died
			var bytes = Serialize(new PeerDiedPacket() { DocumentName = documentName, Peer = link.Peer });
			udp.Send(bytes, bytes.Length);

			// TODO

			// wait until repair is complete

			// resend failedData to all new links

			// declare one peer dead
			ObjectReveived(link, new PeerObject() { Object = new LostPeerPacket(), Peer = Self });
		}

		private void SendPacket(object packet, TcpLink link)
		{
			byte[] data = Serialize(new PeerObject() { Peer = Self, Object = packet });
			Console.WriteLine("TcpLinkControl: Outgoing (" + link + "): " + packet);
			link.Send(data);
		}

		private void BroadcastBytes(byte[] bytes, TcpLink exclude = null)
		{
			lock (links)
				foreach (TcpLink l in links)
					if (l != exclude)
						l.Send(bytes);
		}

		private void ObjectReveived(TcpLink link, object o)
		{
			var po = o as PeerObject;
			Console.WriteLine("TcpLinkControl: Incoming (" + po.Peer.EndPoint + "): " + po.Object);

			// forward
			if (po.Object.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				BroadcastBytes(Serialize(po), link);

			FirePacketArrived(po.Object, po.Peer, p => SendPacket(p, link));
		}

		private void FirePacketArrived(object packet, Peer peer, SendBackFunc sendBack)
		{
			var handler = PacketArrived;
			if (handler != null)
				handler(packet, peer, sendBack);
		}

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		internal bool FindPeer()
		{
			Debug.Assert(!tcpListener.Pending());
			var peerTask = tcpListener.AcceptTcpClientAsync();

			// send a broadcast with the document name into the network
			Console.WriteLine("Broadcasting for " + documentName);
			udp.Client.SendTo(Serialize(new FindPacket() { DocumentName = documentName, ListenPort = tcpListenPort }), new IPEndPoint(IPAddress.Broadcast, broadcastPort));

			// wait for an answer
			Console.WriteLine("Waiting for TCP connect");
			if (peerTask.Wait(linkEstablishTimeoutMs))
			{
				var tcp = peerTask.Result;

				// receive peer's port
				byte[] portBytes = new byte[sizeof(int)];
				Debug.Assert(tcp.GetStream().Read(portBytes, 0, sizeof(int)) == sizeof(int)); // assume an int gets sent at once
				int remotePort = BitConverter.ToInt32(portBytes, 0);

				var address = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address;
				var peerEp = new IPEndPoint(address, remotePort);
				Console.WriteLine("TCP connect from " + peerEp);
				Console.WriteLine("Connection established");
				NewLinkEstablished(new TcpLink(tcp, new Peer() { EndPoint = peerEp }));
				return true;
			}
			else
			{
				Console.WriteLine("No answer. I'm first owner");
				return false;
			}
		}

		private bool IsLocalAddress(IPAddress address)
		{
			return Dns.GetHostAddresses(Dns.GetHostName()).Any(a => a.Equals(address));
		}

		private TcpListener FindTcpListener()
		{
			// find and open TCP listening port for incoming connection
			while (true)
			{
				try
				{
					var l = new TcpListener(IPAddress.Any, tcpListenPort);
					l.Start(1); // only listen for 1 connection
					Console.WriteLine("Started TCP listener on port " + tcpListenPort);
					return l;
				}
				catch (SocketException)
				{
					//Console.WriteLine("Failed to establish TCP listener on port " + tcpListenPort + ": " + e);
					tcpListenPort++;
					if (tcpListenPort >= 0xFFFF)
						throw new Exception("Failed to find a TCP port for listening");
					continue;
				}
			}
		}

		/// <summary>
		/// Listens on the network for new peers with the given document name.
		/// If such a peer connects, the NewLinkEstablished event is fired.
		/// </summary>
		/// <param name="documentName"></param>
		private void StartListeningForPeers()
		{
			tcpListener = FindTcpListener();

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
						Console.WriteLine("Waiting for broadcast");
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
							Console.WriteLine("Received broadcast from {0}: {1}", ep.Address, packet.DocumentName);
							if (packet.DocumentName == documentName)
							{
								if (packet is FindPacket)
								{
									var p = packet as FindPacket;

									if (IsLocalAddress(ep.Address) && p.ListenPort == tcpListenPort)
									{
										Console.WriteLine("Self broadcast detected");
										OwnIPDetected(new IPEndPoint(ep.Address, tcpListenPort));
									}
									else
									{
										// establish connection to peer
										var tcp = new TcpClient();
										var peerEP = new IPEndPoint(ep.Address, p.ListenPort);
										Console.WriteLine("TCP connect to " + peerEP);
										try
										{
											tcp.Connect(peerEP);
											tcp.GetStream().Write(BitConverter.GetBytes(tcpListenPort), 0, sizeof(int));
										}
										catch (Exception e)
										{
											Console.WriteLine("Failed to connet: " + e);
											tcp.Close();
										}

										Console.WriteLine("Connection established");
										NewLinkEstablished(new TcpLink(tcp, new Peer() { EndPoint = peerEP }));
									}
								}
								else if (packet is PeerDiedPacket)
								{
									var p = packet as PeerDiedPacket;

									Console.WriteLine("Received panic packet");

									// check all links if they are affected and kill affected ones
									bool foundDead = false;
									lock (links)
									{
										var failedPeer = links.Where(l => l.Peer.Equals(p.Peer)).FirstOrDefault();
										if (failedPeer != null)
										{
											foundDead = true;
											failedPeer.Close();
											repairModeWaitHandle.Reset(); // start panic mode
										}
									}


								}
							}
							else
								Console.WriteLine("Document mismatch");
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
	}
}
