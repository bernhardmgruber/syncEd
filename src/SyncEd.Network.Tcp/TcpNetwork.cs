using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

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
			p.Start();
		}

		void PeerFailed(TcpLink link)
		{
			lock (peers)
				peers.Remove(link);
			link.Close();
			Panic(link);
		}

		byte[] Serialize(object o)
		{
			using (var ms = new MemoryStream())
			{
				// keep place for an int
				//ms.Position = sizeof(int);

				// serialize packet
				var f = new BinaryFormatter();
				f.Serialize(ms, o);

				// write length of serialized data
				//ms.Write(BitConverter.GetBytes((int)ms.Length - sizeof(int)), 0, sizeof(int));

				byte[] bytes = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(bytes, 0, (int)ms.Length);

				return bytes;
			}
		}

		public void SendPacket(object packet, Peer peer = null)
		{
			byte[] data = Serialize(packet);
			if (peer == null)
			{
				Console.WriteLine("TcpLinkControl: Outgoing (" + peers.Count + "): " + packet.ToString());
				BroadcastBytes(data);
			}
			else
			{
				Console.WriteLine("TcpLinkControl: Outgoing (" + peer.ToString() + "): " + packet.ToString());
				SendBytes(data, peer);
			}
		}

		void SendBytes(byte[] bytes, Peer peer)
		{
			lock (peers)
				peers.Find(tcpPeer => tcpPeer.Peer == peer).Send(bytes);
		}

		void BroadcastBytes(byte[] bytes, Peer exclude = null)
		{
			lock (peers)
				foreach (TcpLink p in peers)
					if (p.Peer != exclude)
						p.Send(bytes);
		}

		void ObjectReveived(object o, Peer peer)
		{
			Console.WriteLine("TcpLinkControl: Incoming: " + o.ToString());

			// forward
			if (o.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				BroadcastBytes(Serialize(o), peer);

			PacketArrived(o, peer);
		}

		void Panic(TcpLink deadPeer)
		{
			Console.WriteLine("PANIC - " + deadPeer.Peer.Address + " is dead");
		}
	}
}
