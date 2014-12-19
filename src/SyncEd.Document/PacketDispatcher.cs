using System;
using SyncEd.Network;
using SyncEd.Network.Packets;

namespace SyncEd.Document
{
	public class PacketDispatcher
	{
		public event Action<LostPeerPacket, Peer, SendBackFunc> LostPeerPacketArrived;
		public event Action<NewPeerPacket, Peer, SendBackFunc> NewPeerPacketArrived;
		public event Action<PeerCountPacket, Peer, SendBackFunc> PeerCountPacketArrived;
		public event Action<QueryPeerCountPacket, Peer, SendBackFunc> QueryPeerCountPacketArrived;
		public event Action<UpdateCaretPacket, Peer, SendBackFunc> UpdateCaretPacketArrived;
		public event Action<AddTextPacket, Peer, SendBackFunc> AddTextPacketArrived;
		public event Action<DeleteTextPacket, Peer, SendBackFunc> DeleteTextPacketArrived;
		public event Action<DocumentPacket, Peer, SendBackFunc> DocumentPacketArrived;
		public event Action<QueryDocumentPacket, Peer, SendBackFunc> QueryDocumentPacketArrived;

		public PacketDispatcher(INetwork network)
		{
			network.PacketArrived += DispatchPacket;
		}

		bool TryFire<P>(object packet, Peer peer, SendBackFunc sendBack, Action<P, Peer, SendBackFunc> handler)
		{
			if (packet is P && handler != null)
			{
				handler((P)packet, peer, sendBack);
				return true;
			}
			else
				return false;
		}

		void DispatchPacket(object packet, Peer peer, SendBackFunc sendBack)
		{
			// dispatch to UI
			if (false) { }
			else if (TryFire(packet, peer, sendBack, LostPeerPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, NewPeerPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, PeerCountPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, QueryPeerCountPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, UpdateCaretPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, AddTextPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, DeleteTextPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, DocumentPacketArrived)) {}
			else if (TryFire(packet, peer, sendBack, QueryDocumentPacketArrived)) {}
			else Console.WriteLine("Unrecognized packet of type: " + packet.GetType().AssemblyQualifiedName);
		}
	}
}
