using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp.CompleteGraph
{
	public class CompleteGraphNetwork : BasicNetwork
	{
		public override void SendPacket(object packet)
		{
			base.SendPacket(packet);
			tcpNetwork.BroadcastObject(new TcpObject() { Peer = Self, Object = packet });
		}

		protected override bool ProcessCustomTcpObject(TcpLink link, TcpObject o)
		{
			if (o.Object is ConnectPeerPacket)
			{
				var p = (o.Object as ConnectPeerPacket);
				if (tcpNetwork.EstablishConnectionTo(p.Peer.EndPoint) == null)
					Console.WriteLine("FATAL: Could not connect peer: " + p.Peer);
			}
			else if (o.Object is PeerCountPacket)
			{
				var p = (o.Object as PeerCountPacket);
				for (int i = 0; i < p.Count; i++)
					if (!tcpNetwork.WaitForTcpConnect())
						Console.WriteLine("FATAL: Expected incoming connection which never came");
			}
			else
				return true;
			return false;
		}

		protected override void ProcessCustomUdpObject(System.Net.IPEndPoint endpoint, UdpObject o)
		{
			throw new NotImplementedException();
		}

		protected override void ConnectedPeer(Peer peer)
		{
			int count = 0;
			lock(tcpNetwork.Links)
				count = tcpNetwork.Links.Count;

			// tell new peers how many other peers will connect
			tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new PeerCountPacket() { Count = count - 1 } }, l => l.Peer.Equals(peer));

			// tell other peers to connect this new peer
			tcpNetwork.MulticastObject(new TcpObject() { Peer = Self, Object = new ConnectPeerPacket() { Peer = peer } }, l => !l.Peer.Equals(peer));
		}
	}
}
