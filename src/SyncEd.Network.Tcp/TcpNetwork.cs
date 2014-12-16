using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace SyncEd.Network.Tcp
{
	public class TcpNetwork : INetwork
	{
		public event PacketHandler PacketArrived;

		private TcpLinkEstablisher establisher;
		private List<TcpLink> peers;
		private BlockingCollection<Tuple<object, TcpLink>> packets;

		/// <summary>
		/// Starts the link control system which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		public bool Start(string documentName)
		{
			establisher = new TcpLinkEstablisher(documentName);
			peers = new List<TcpLink>();
			establisher.NewLinkEstablished += NewLinkEstablished;
			return establisher.FindPeer();
		}

		public void Stop()
		{
			establisher.Close();
			peers.ForEach(p => p.Close());
			establisher = null;
			peers = new List<TcpLink>();
			packets = new BlockingCollection<Tuple<object, TcpLink>>();
		}

		void NewLinkEstablished(TcpLink p)
		{
			lock (peers)
				peers.Add(p);
			p.ObjectReceived += ObjectReveived;
			p.Failed += PeerFailed;
		}

		void PeerFailed(TcpLink sender)
		{
			lock (peers)
				peers.Remove(sender);
			sender.Close();
			Panic(sender);
		}

		public void SendPacket(object packet, Peer peer = null)
		{
			if (peer == null)
				BroadcastObject(packet);
			else
				SendObjectTo(packet, peer);
		}

		void SendObjectTo(object o, Peer peer)
		{
			Console.WriteLine("TcpLinkControl: Outgoing (" + peer.ToString() + "): " + o.ToString());

			lock (peers)
				peers.Find(tcpPeer => tcpPeer.Peer == peer).SendAsync(o);
		}

		void BroadcastObject(object o, Peer exclude = null)
		{
			Console.WriteLine("TcpLinkControl: Outgoing (" + peers.Count + "): " + o.ToString());

			lock (peers)
				foreach (TcpLink p in peers)
					if (p.Peer != exclude)
						p.SendAsync(o);
		}

		void ObjectReveived(object o, Peer peer)
		{
			Console.WriteLine("TcpLinkControl: Incoming: " + o.ToString());

			// forward
			if (o.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				BroadcastObject(o, peer);

			PacketArrived(o, peer);
		}

		void Panic(TcpLink deadPeer)
		{
			Console.WriteLine("PANIC - " + deadPeer.Peer.Address + " is dead");

		}
	}
}
