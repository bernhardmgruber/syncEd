using SyncEd.Network.Packets;

namespace SyncEd.Network
{
	public delegate void PacketHandler(object packet, Peer peer);

	public interface INetwork
	{
		event PacketHandler PacketArrived;

		/// <summary>
		/// Starts the network subsystem which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		bool Start(string documentName);

		/// <summary>
		/// Stops the network subsystem
		/// </summary>
		void Stop();

		/// <summary>
		/// Sends a packet into the network
		/// </summary>
		/// <param name="packet">Packet to send, can be any object</param>
		/// <param name="peer">Optional. Receiver of the packet. If null is specified, the packet is broadcasted</param>
		void SendPacket(object packet, Peer peer = null);
	}
}
