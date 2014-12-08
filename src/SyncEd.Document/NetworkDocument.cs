using System;
using System.Threading.Tasks;
using SyncEd.Network;
using SyncEd.Network.Packets;

namespace SyncEd.Document
{
    public class NetworkDocument
        : IDocument
    {
        private readonly INetwork network;

        public NetworkDocument(INetwork network)
        {
            this.network = network;
            network.AddTextPacketArrived += network_AddTextPacketArrived;
            network.DeleteTextPacketArrived += network_DeleteTextPacketArrived;
            network.DocumentPacketArrived += network_DocumentPacketArrived;
            network.QueryDocumentPacketArrived += network_QueryDocumentPacketArrived;
        }

        void network_QueryDocumentPacketArrived(QueryDocumentPacket packet, Peer peer)
        {
            throw new NotImplementedException();
        }

        void network_DocumentPacketArrived(DocumentPacket packet, Peer peer)
        {
            throw new NotImplementedException();
        }

        void network_DeleteTextPacketArrived(DeleteTextPacket packet, Peer peer)
        {
            throw new NotImplementedException();
        }

        void network_AddTextPacketArrived(AddTextPacket packet, Peer peer)
        {
            throw new NotImplementedException();
        }

        public bool IsConnected { get; private set; }

        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            return Task.Run(() => network.Start(documentName));
        }

        public Task Close()
        {
            return Task.Run(() => network.Stop());
        }

        public void ChangeText(int offset, int length, string text)
        {
            if (length == 0)
                // text added
                network.SendPacket(new AddTextPacket() { Offset = offset, Text = text });
            else
                network.SendPacket(new DeleteTextPacket() { Offset = offset, Length = length });
        }

        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;
    }
}