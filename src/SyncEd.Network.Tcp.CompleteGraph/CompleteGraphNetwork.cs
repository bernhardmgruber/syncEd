using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			if (found)
			{
				Thread.Sleep(TcpBroadcastNetwork.connectTimeoutMs);
				tcpNetwork.KillPending();
				tcpNetwork.BroadcastObject(new TcpObject() { Peer = Self, Object = new RequestInvitePacket() });
				connectFinishedEvent.WaitOne(); // wait to receive PeersToConnectPacket and connect to full graph before proceeding
			}
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
			if (o.Object is ConnectToPeerPacket)
			{
				var p = o.Object as ConnectToPeerPacket;
				if (tcpNetwork.EstablishConnectionTo(p.Peer.EndPoint) == null)
					Log.WriteLine("Warning: Could not connect new peer " + p.Peer);
				//if (!tcpNetwork.WaitForTcpConnect())
				//	Log.WriteLine("FATAL: Expected incoming connection from: " + p.Peer);
			}
			else if (o.Object is PeerCountPacket)
			{
				var p = o.Object as PeerCountPacket;

				while (true)
				{
					lock(tcpNetwork.Links)
						if (tcpNetwork.Links.Count >= p.Count)
						{
							Debug.Assert(tcpNetwork.Links.Count == p.Count, "Too many connections. There must be a duplicate.");
							break;
						}

					Debug.Assert(tcpNetwork.WaitForTcpConnect(), "failed to wait for a connect during network construction");
				}

				//Log.WriteLine("Connecting to " + p.Peers.Length + " peers");
				//foreach(var peer in p.Peers)
				//	if (tcpNetwork.EstablishConnectionTo(peer.EndPoint) == null)
				//		Log.WriteLine("FATAL: Could not connect to peer: " + peer);
				//Log.WriteLine("Established connections to " + p.Peers.Length + " peers");
				connectFinishedEvent.Set();
			}
			else if (o.Object is RequestInvitePacket)
			{
				var p = o.Object as RequestInvitePacket;

				Log.WriteLine("Inviting new peer to network: " + o.Peer);

				int count = 0;
				lock (tcpNetwork.Links)
					count = tcpNetwork.Links.Count;

				// tell new peer for how many connections it should have
				tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new PeerCountPacket() { Count = count } }, l => l.Peer.Equals(o.Peer));

				// tell other peers to connet to the new peer
				tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new ConnectToPeerPacket() { Peer = o.Peer } }, l => !l.Peer.Equals(o.Peer));
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
		}
	}
}
