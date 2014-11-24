using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace SyncEd.Network
{
	public delegate void PacketArrivedHandler(Packet p);

	public class LinkControl
	{
		public event PacketArrivedHandler PacketArrived;

		public LinkEstablisher Establisher { get; set; }

		private List<Peer> peers = new List<Peer>();
		public IList<Peer> Peers { get { return peers; } }

		private BlockingCollection<Packet> packets = new BlockingCollection<Packet>();

		private CancellationTokenSource cancelSrc;

		public LinkControl(LinkEstablisher establisher)
		{
			Establisher = establisher;
			Establisher.NewLinkEstablished += NewLinkEstablished;
		}

		void NewLinkEstablished(Peer p)
		{
			peers.Add(p);

			var packetTask = Task.Run(() =>
			{
				while (true)
				{
					lock (p)
					{
						if (!p.Tcp.Connected)
							break;

						var f = new BinaryFormatter();
						var packet = (Packet)f.Deserialize(p.Tcp.GetStream());
						packets.Add(packet);
					}

					p.Tcp.GetStream().Close();
					p.Tcp.Close();
					Console.WriteLine("Lost peer " + p);

					// TODO, reconnect network
				}
			});
		}

		void Start(string documentName)
		{
			cancelSrc = new CancellationTokenSource();
			var token = cancelSrc.Token;

			Establisher.FindPeer(documentName);
			Establisher.ListenForPeers(documentName, token);

			Task.Run(() =>
			{
				while (!token.IsCancellationRequested)
				{
					var packet = packets.Take(token);
					if (PacketArrived != null)
						PacketArrived(packet);
				}
			});
		}

		void Stop()
		{
			cancelSrc.Cancel();

			peers.ForEach(p =>
			{
				lock (p)
				{
					p.Tcp.Client.Disconnect(false);
				}
			});
		}
	}
}
