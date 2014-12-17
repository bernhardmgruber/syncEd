using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace SyncEd.Network.Tcp
{
	public class TcpNetwork : INetwork
	{
		[Serializable]
		private class PeerObject
		{
			internal Peer Peer { get; set; }
			internal object Object { get; set; }

			public override string ToString()
			{
				return "PeerObject {" + Peer + ", " + Object + "}";
			}
		}

		public event PacketHandler PacketArrived;

		private TcpLinkEstablisher establisher;
		private List<TcpLink> links;
		private BlockingCollection<Tuple<object, TcpLink>> packets;

		public Peer Self { get; private set; }

		/// <summary>
		/// Starts the link control system which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		public bool Start(string documentName)
		{
			links = new List<TcpLink>();
			establisher = new TcpLinkEstablisher(documentName);
			establisher.NewLinkEstablished += NewLinkEstablished;
			establisher.OwnIPDetected += OwnIPDetected;
			return establisher.FindPeer();
		}

		public void Stop()
		{
			establisher.Close();
			links.ForEach(p => p.Close());
			establisher = null;
			links = new List<TcpLink>();
			packets = new BlockingCollection<Tuple<object, TcpLink>>();
		}
		private void OwnIPDetected(IPAddress address)
		{
			Self = new Peer() { Address = address };
		}

		private void NewLinkEstablished(TcpLink p)
		{
			lock (links)
				links.Add(p);
			p.ObjectReceived += ObjectReveived;
			p.Failed += PeerFailed;
			p.Start();
		}

		private void PeerFailed(TcpLink link)
		{
			lock (links)
				links.Remove(link);
			link.Close();
			Panic(link);
		}

		private byte[] Serialize(object o)
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
			Debug.Assert(Self != null, "Own IP has not been determined for send");

			byte[] data = Serialize(new PeerObject() { Peer = Self, Object = packet });
			if (peer == null)
			{
				Console.WriteLine("TcpLinkControl: Outgoing (" + links.Count + "): " + packet.ToString());
				BroadcastBytes(data);
			}
			else
			{
				Console.WriteLine("TcpLinkControl: Outgoing (" + peer.ToString() + "): " + packet.ToString());
				SendBytes(data, peer);
			}
		}

		private void SendBytes(byte[] bytes, Peer peer)
		{
			lock (links)
			{
				var link = links.Find(tcpPeer => tcpPeer.Address.Equals(peer.Address));
				if (link == null)
					Console.WriteLine("Warning: Sending packages to peers which are not directly connected is not supported. Dropping package");
				else
					link.Send(bytes);
			}
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
			Console.WriteLine("TcpLinkControl: Incoming: " + o.ToString());

			var po = o as PeerObject;

			// forward
			if (po.Object.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				BroadcastBytes(Serialize(po), link);

			PacketArrived(po.Object, po.Peer);
		}

		private void Panic(TcpLink deadLink)
		{
			Console.WriteLine("PANIC - " + deadLink + " is dead");
		}
	}
}
