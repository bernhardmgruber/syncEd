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
        private readonly INetwork network;

        private readonly StringBuilder documentText;

        public NetworkDocument(INetwork network)
        {
            this.network = network;

            documentText = new StringBuilder();

            network.AddTextPacketArrived += network_AddTextPacketArrived;
            network.DeleteTextPacketArrived += network_DeleteTextPacketArrived;
            network.DocumentPacketArrived += network_DocumentPacketArrived;
            network.QueryDocumentPacketArrived += network_QueryDocumentPacketArrived;
        }

        public bool IsConnected { get; private set; }

        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException("Cannot connect when document is already connected.");

            documentText.Clear();
            FireTextChanged();

            return Task.Run(() => {
                bool succes = network.Start(documentName);
                IsConnected = true;
                return true; // we are always connected
            });
        }

        public Task Close()
        {
            return Task.Run(() => {
                if (IsConnected)
                    network.Stop();
            });
        }

        private void network_AddTextPacketArrived(AddTextPacket packet, Peer peer)
        {
            lock (documentText) {
                documentText.Insert(packet.Offset, packet.Text);
            }
            FireTextChanged();
        }

        private void network_DeleteTextPacketArrived(DeleteTextPacket packet, Peer peer)
        {
            lock (documentText) {
                documentText.Remove(packet.Offset, packet.Length);
            }
            FireTextChanged();
        }

        private void network_QueryDocumentPacketArrived(QueryDocumentPacket packet, Peer peer)
        {
            network.SendPacket(new DocumentPacket() { Document = documentText.ToString() });
        }

        private void network_DocumentPacketArrived(DocumentPacket packet, Peer peer)
        {
            lock (documentText) {
                documentText.Clear();
                documentText.Append(packet.Document);
            }
            FireTextChanged();
        }

        public void ChangeText(int offset, int length, string text, bool guiSource)
        {
            if (length > 0)
                network.SendPacket(new DeleteTextPacket() { Offset = offset, Length = length });
            if (text.Length > 0)
                network.SendPacket(new AddTextPacket() { Offset = offset, Text = text });

            FireTextChanged(guiSource);
        }

        protected void FireTextChanged(bool guiSource = false)
        {
            if (!guiSource) {
                string text = documentText.ToString();
                if (TextChanged != null)
                    TextChanged(this, new DocumentTextChangedEventArgs(text, guiSource));
            }
        }

        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;
    }
}