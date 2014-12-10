using System;
using System.Threading.Tasks;
using SyncEd.Network;
using SyncEd.Network.Packets;
using System.Text;

namespace SyncEd.Document
{
    public class NetworkDocument
        : IDocument
    {
        private readonly StringBuilder documentText;
        private readonly INetwork network;

        public NetworkDocument(INetwork network)
        {
            this.network = network;
            this.documentText = new StringBuilder();
            network.AddTextPacketArrived += network_AddTextPacketArrived;
            network.DeleteTextPacketArrived += network_DeleteTextPacketArrived;
            network.DocumentPacketArrived += network_DocumentPacketArrived;
            network.QueryDocumentPacketArrived += network_QueryDocumentPacketArrived;
        }

        void network_QueryDocumentPacketArrived(QueryDocumentPacket packet, Peer peer)
        {
            network.SendPacket(new DocumentPacket() { Document = documentText.ToString() });
        }

        void network_DocumentPacketArrived(DocumentPacket packet, Peer peer)
        {
            documentText.Clear();
            documentText.Append(packet.Document);
            FireTextChanged();
        }

        void network_DeleteTextPacketArrived(DeleteTextPacket packet, Peer peer)
        {
            documentText.Remove(packet.Offset, packet.Length);
            FireTextChanged();
        }

        void network_AddTextPacketArrived(AddTextPacket packet, Peer peer)
        {
            documentText.Insert(packet.Offset, packet.Text);
            FireTextChanged();
        }

        public bool IsConnected { get; private set; }

        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            var result = Task.Run(() => network.Start(documentName));

            documentText.Clear();
            FireTextChanged();

            IsConnected = true;

            //return result;
            return Task.Run(() => true); // we are always connected
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
            FireTextChanged();
        }

        protected void FireTextChanged()
        {
            string text = documentText.ToString();
            if (TextChanged != null)
                TextChanged(this, new DocumentTextChangedEventArgs(text));
        }

        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;
    }
}