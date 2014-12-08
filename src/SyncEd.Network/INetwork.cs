using SyncEd.Network.Packets;

namespace SyncEd.Network
{
    public delegate void PacketArrivedHandler(Packet packet, Peer peer);

    public interface INetwork
    {
        event PacketArrivedHandler PacketArrived;

        /// <summary>
        /// Starts the network subsystem which is responsible for managing links and packets
        /// </summary>
        /// <returns>Returns true if a peer could be found for the given document name</returns>
        bool Start(string documentName);
        void Stop();
        void SendPacket(Packet packet);
    }
}
