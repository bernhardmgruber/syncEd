using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyncEd.Network;
using SyncEd.Network.Packets;
using System.Text;

namespace SyncEd.Document
{
    public class NetworkDocument
        : IDocument
    {
        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;
        public event EventHandler<CaretChangedEventArgs> CaretChanged;

        private readonly INetwork network;
        private readonly StringBuilder documentText;
        private readonly IDictionary<Peer, int?> carets;

        public NetworkDocument(INetwork network)
        {
            this.network = network;

            documentText = new StringBuilder();
            carets = new Dictionary<Peer, int?>();

            var dispatcher = new PacketDispatcher(network);
            dispatcher.AddTextPacketArrived += network_AddTextPacketArrived;
            dispatcher.DeleteTextPacketArrived += network_DeleteTextPacketArrived;
            dispatcher.DocumentPacketArrived += network_DocumentPacketArrived;
            dispatcher.QueryDocumentPacketArrived += network_QueryDocumentPacketArrived;
            dispatcher.UpdateCaretPacketArrived += network_UpdateCaretPackageArrived;
        }

        public bool IsConnected { get; private set; }

        public void Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException("Cannot connect when document is already connected.");

            bool foundPeer = network.Start(documentName);
            if (foundPeer)
                network.SendPacket(new QueryDocumentPacket());

            IsConnected = true;
            documentText.Clear();
            FireTextChanged();
        }

        public void Close()
        {
            if (IsConnected)
                network.Stop();
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
            network.SendPacket(new DocumentPacket() { Document = documentText.ToString() }, peer);
        }

        private void network_DocumentPacketArrived(DocumentPacket packet, Peer peer)
        {
            lock (documentText) {
                documentText.Clear();
                documentText.Append(packet.Document);
            }
            FireTextChanged();
        }

        private void network_UpdateCaretPackageArrived(UpdateCaretPacket packet, Peer peer)
        {
            FireCaretPositionChanged(peer, packet.Position);
        }

        // is called when the text is changed by the UI
        public void ChangeText(int offset, int length, string text)
        {
            lock (documentText)
            {
                if (length > 0)
                {
                    documentText.Remove(offset, length);
                    network.SendPacket(new DeleteTextPacket() { Offset = offset, Length = length });
                }
                if (text.Length > 0)
                {
                    documentText.Insert(offset, text);
                    network.SendPacket(new AddTextPacket() { Offset = offset, Text = text });
                }
            }
        }

        public void ChangeCaretPos(int pos)
        {
            network.SendPacket(new UpdateCaretPacket() { Position = pos });
        }

        protected void FireTextChanged()
        {
            string text = documentText.ToString();
            if (TextChanged != null)
                TextChanged(this, new DocumentTextChangedEventArgs(text));
        }

        protected void FireCaretPositionChanged(Peer peer, int? position)
        {
            if (CaretChanged != null) {
                CaretChanged(this, new CaretChangedEventArgs(peer, position));
            }
        }



    }
}