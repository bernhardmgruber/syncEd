using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp.CompleteGraph
{
	public class CompleteGraphNetwork : BasicNetwork
	{
		private ManualResetEvent connectFinishedEvent;

		public override bool Start(string documentName)
		{
			connectFinishedEvent = new ManualResetEvent(false);
			var found = base.Start(documentName);
			if(found)
				connectFinishedEvent.WaitOne(); // wait to receive PeersToConnectPacket and connect to full graph before proceeding
			connectFinishedEvent.Dispose();
			return found;
		}

		public override void SendPacket(object packet)
		{
			base.SendPacket(packet);
			tcpNetwork.BroadcastObject(new TcpObject() { Peer = Self, Object = packet });
		}

		protected override bool ProcessCustomTcpObject(TcpLink link, TcpObject o)
		{
			if (o.Object is ExpectNewPeerPacket)
			{
				var p = (o.Object as ExpectNewPeerPacket);
				if (!tcpNetwork.WaitForTcpConnect())
					Console.WriteLine("FATAL: Expected incoming connection from: " + p.Peer);
			}
			else if (o.Object is PeersToConnectPacket)
			{
				var p = (o.Object as PeersToConnectPacket);
				Console.WriteLine("Connecting to " + p.Peers.Length + " peers");
				foreach(var peer in p.Peers)
					if (tcpNetwork.EstablishConnectionTo(peer.EndPoint) == null)
						Console.WriteLine("FATAL: Could not connect to peer: " + peer);
				Console.WriteLine("Established connections to " + p.Peers.Length + " peers");
				connectFinishedEvent.Set();
			}
			else
				return true;
			return false;
		}

		protected override void ProcessCustomUdpObject(System.Net.IPEndPoint endpoint, UdpObject o)
		{
		}

		protected override void PeerFailed(TcpLink link, byte[] failedData)
		{
			base.PeerFailed(link, failedData);
			FirePacketArrived(new LostPeerPacket(), link.Peer, p => { });
		}

		protected override void ConnectedPeer(Peer peer)
		{
			Peer[] peers = null;
			lock (tcpNetwork.Links)
				peers = tcpNetwork.Links.Where(l => !l.Peer.Equals(peer)).Select(l => l.Peer).ToArray();

			// tell new peer to which peers he has to connect
			tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new PeersToConnectPacket() { Peers = peers } }, l => l.Peer.Equals(peer));

			// tell other peers to except a connection from this new peer
			tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new ExpectNewPeerPacket() { Peer = peer } }, l => !l.Peer.Equals(peer));
		}
	}
}
