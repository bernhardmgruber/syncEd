using SyncEd.Network.Packets;
using SyncEd.Network.Tcp;
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

namespace SyncEd.Network.Tcp.SpanningTree
{
	public class SpanningTreeNetwork : INetwork
	{
		public event PacketHandler PacketArrived;

		private string documentName;

		private List<Tuple<PeerObject, TcpLink>> repairModeOutgoingTcpPacketBuffer;
		private SortedSet<Peer> repairMasterPeers;
		private Peer repairDeadPeer;

		private bool InRepairMode { get { return repairDeadPeer != null; } }

		private const int repairMasterNodeWaitMs = 200;
		private const int repairReestablishWaitMs = 1000;

		private const int broadcastPort = 1337; // UDP port for sending broadcasts

		private UdpBroadcastNetwork udpNetwork;
		private TcpBroadcastNetwork tcpNetwork;

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

			udpNetwork = new UdpBroadcastNetwork(broadcastPort, (o, e) => ProcessUdpPacket((UdpPacket)o, e));
			tcpNetwork = new TcpBroadcastNetwork((l, o) => TcpObjectReveived(l, o), (l, d) => PeerFailed(l, d));

			bool found = FindPeer();
			ownIPWaitHandle.WaitOne(); // wait for listener thread to receive self broadcast and determine own IP
			return found;
		}

		public void Stop()
		{
			udpNetwork.Stop();
			tcpNetwork.Dispose();

			ownIPWaitHandle.Dispose();
		}

		public void SendPacket(object packet)
		{
			Debug.Assert(Self != null, "Own IP has not been determined for send");
			TcpBroadcastObject(new PeerObject() { Peer = Self, Object = packet });
		}
		#endregion

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		internal bool FindPeer()
		{
			// send a broadcast with the document name into the network
			Console.WriteLine("Broadcasting for " + documentName);
			udpNetwork.BroadcastObject(new FindPacket() { DocumentName = documentName, ListenPort = tcpNetwork.ListenPort });

			// wait for an answer
			var r = tcpNetwork.WaitForTcpConnect();
			if (!r)
				Console.WriteLine("No answer. I'm first owner");
			return r;
		}

		private void OwnIPDetected(IPEndPoint address)
		{
			Self = new Peer() { EndPoint = address };
			Console.WriteLine("Own IP determined as " + address);
			ownIPWaitHandle.Set();
		}

		private void PeerFailed(TcpLink link, byte[] failedData)
		{
			Console.WriteLine("PANIC - " + link + " is dead");
			RepairDeadLink(link, Self);

			// inform peers that a link died
			udpNetwork.BroadcastObject(new PeerDiedPacket() { DocumentName = documentName, DeadPeer = link.Peer, RepairPeer = Self });
		}

		private void TcpBroadcastObject(PeerObject po, TcpLink exclude = null, bool overrideRepair = false)
		{
			if (!overrideRepair && InRepairMode)
			{
				Console.WriteLine("Buffered: " + po.Object);
				repairModeOutgoingTcpPacketBuffer.Add(Tuple.Create(po, exclude));
			}
			else
				tcpNetwork.BroadcastObject(po, exclude);
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
				link.Send(Utils.Serialize(new PeerObject() { Peer = Self, Object = p }));
			});
		}

		private void FirePacketArrived(object packet, Peer peer, SendBackFunc sendBack)
		{
			var handler = PacketArrived;
			if (handler != null)
				handler(packet, peer, sendBack);
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
			if (Utils.IsLocalAddress(endpoint.Address) && p.ListenPort == tcpNetwork.ListenPort)
				OwnIPDetected(new IPEndPoint(endpoint.Address, tcpNetwork.ListenPort));
			else
				tcpNetwork.EstablishConnectionTo(new IPEndPoint(endpoint.Address, p.ListenPort));
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
				lock (tcpNetwork.Links)
					deadLink = tcpNetwork.Links.Where(l => l.Peer.Equals(p.DeadPeer)).FirstOrDefault();
				Console.WriteLine("All links ok: " + (deadLink == null));
				if (deadLink != null)
					RepairDeadLink(deadLink, p.RepairPeer);
			}
		}

		private void RepairDeadLink(TcpLink deadLink, Peer repairMasterPeer)
		{
			lock (tcpNetwork.Links)
				tcpNetwork.Links.Remove(deadLink);
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
				tcpNetwork.EstablishConnectionTo(masterNode.EndPoint);
			}
			else
			{
				Console.WriteLine("Waiting for incoming connections.");
				var sw = Stopwatch.StartNew();
				while (sw.ElapsedMilliseconds < repairReestablishWaitMs)
					tcpNetwork.WaitForTcpConnect();
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
			lock (repairMasterPeers)
				repairMasterPeers.Clear();
			repairDeadPeer = null;

			Console.WriteLine("Repair finished");
		}
	}
}
