using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network
{
	public delegate void DocumentPacketHandler(DocumentPacket packet, Peer peer);
	public delegate void QueryDocumentPacketHandler(QueryDocumentPacket packet, Peer peer);
	public delegate void AddTextPacketHandler(AddTextPacket packet, Peer peer);
	public delegate void DeleteTextPacketHandler(DeleteTextPacket packet, Peer peer);
	public delegate void UpdateCaretPacketHandler(UpdateCaretPacket packet, Peer peer);
	public delegate void NewPeerPacketHandler(NewPeerPacket packet, Peer peer);
	public delegate void LostPeerPacketHandler(LostPeerPacket packet, Peer peer);
	public delegate void QueryPeerCountPacketHandler(QueryPeerCountPacket packet, Peer peer);
	public delegate void PeerCountPacketHandler(PeerCountPacket packet, Peer peer);

	public class PacketDispatcher
	{
		public event DocumentPacketHandler DocumentPacketArrived;
		public event QueryDocumentPacketHandler QueryDocumentPacketArrived;
		public event AddTextPacketHandler AddTextPacketArrived;
		public event DeleteTextPacketHandler DeleteTextPacketArrived;
		public event UpdateCaretPacketHandler UpdateCaretPacketArrived;
		public event NewPeerPacketHandler NewPeerPacketArrived;
		public event LostPeerPacketHandler LostPeerPacketArrived;
		public event QueryPeerCountPacketHandler QueryPeerCountPacketArrived;
		public event PeerCountPacketHandler PeerCountPacketArrived;

		public PacketDispatcher(INetwork network)
		{
			network.PacketArrived += DispatchPacket;
		}

		bool TryFire<P>(object packet, Peer peer, Action<P, Peer> handler)
		{
			if (packet is P && handler != null)
			{
				handler((P)packet, peer);
				return true;
			}
			else
				return false;
		}

		void DispatchPacket(object packet, Peer peer)
		{
			// dispatch to UI
			if (packet is AddTextPacket && AddTextPacketArrived != null)
				AddTextPacketArrived(packet as AddTextPacket, peer);
			else if (packet is DeleteTextPacket && DeleteTextPacketArrived != null)
				DeleteTextPacketArrived(packet as DeleteTextPacket, peer);
			else if (packet is DocumentPacket && DocumentPacketArrived != null)
				DocumentPacketArrived(packet as DocumentPacket, peer);
			else if (packet is QueryDocumentPacket && QueryDocumentPacketArrived != null)
				QueryDocumentPacketArrived(packet as QueryDocumentPacket, peer);
			else if (packet is UpdateCaretPacket && UpdateCaretPacketArrived != null)
				UpdateCaretPacketArrived(packet as UpdateCaretPacket, peer);
			else if (packet is NewPeerPacket && NewPeerPacketArrived != null)
				NewPeerPacketArrived(packet as NewPeerPacket, peer);
			else if (packet is LostPeerPacket && LostPeerPacketArrived != null)
				LostPeerPacketArrived(packet as LostPeerPacket, peer);
			else if (packet is QueryPeerCountPacket && QueryPeerCountPacketArrived != null)
				QueryPeerCountPacketArrived(packet as QueryPeerCountPacket, peer);
			else if (packet is PeerCountPacket && PeerCountPacketArrived != null)
				PeerCountPacketArrived(packet as PeerCountPacket, peer);
			else
				Console.WriteLine("Unrecognized packet of type: " + packet.GetType().AssemblyQualifiedName);
		}
	}
}
