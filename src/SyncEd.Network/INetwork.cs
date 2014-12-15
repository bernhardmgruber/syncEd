using SyncEd.Network.Packets;

namespace SyncEd.Network
{
    public delegate void DocumentPacketHandler(DocumentPacket packet, Peer peer);
    public delegate void QueryDocumentPacketHandler(QueryDocumentPacket packet, Peer peer);
    public delegate void AddTextPacketHandler(AddTextPacket packet, Peer peer);
    public delegate void DeleteTextPacketHandler(DeleteTextPacket packet, Peer peer);
    public delegate void UpdateCaretPacketHandler(UpdateCaretPacket packet, Peer peer);

    public interface INetwork
    {
        event DocumentPacketHandler DocumentPacketArrived;
        event QueryDocumentPacketHandler QueryDocumentPacketArrived;
        event AddTextPacketHandler AddTextPacketArrived;
        event DeleteTextPacketHandler DeleteTextPacketArrived;
        event UpdateCaretPacketHandler UpdateCaretPacketArrived;

        /// <summary>
        /// Starts the network subsystem which is responsible for managing links and packets
        /// </summary>
        /// <returns>Returns true if a peer could be found for the given document name</returns>
        bool Start(string documentName);
        void Stop();
        void SendPacket(DocumentPacket packet);
        void SendPacket(QueryDocumentPacket packet);
        void SendPacket(AddTextPacket packet);
        void SendPacket(DeleteTextPacket packet);
        void SendPacket(UpdateCaretPacket packet);
    }
}
