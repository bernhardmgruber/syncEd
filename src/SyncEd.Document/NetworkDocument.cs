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
        public event EventHandler<PeerCountChangedEventArgs> PeerCountChanged;

        private readonly INetwork network;
        private readonly StringBuilder documentText;
        private readonly IDictionary<Peer, int?> carets;

        private int peerCount = 1; // initially, there is only me :)

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
            dispatcher.NewPeerPacketArrived += dispatcher_NewPeerPacketArrived;
            dispatcher.LostPeerPacketArrived += dispatcher_LostPeerPacketArrived;
            dispatcher.QueryPeerCountPacketArrived += dispatcher_QueryPeerCountPacketArrived;
            dispatcher.PeerCountPacketArrived += dispatcher_PeerCountPacketArrived;
        }

        public bool IsConnected { get; private set; }

        public void Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException("Cannot connect when document is already connected.");

            bool foundPeer = network.Start(documentName);
            if (foundPeer)
            {
                network.SendPacket(new NewPeerPacket()); // neighbors add me to their count
                network.SendPacket(new QueryDocumentPacket()); 
                network.SendPacket(new QueryPeerCountPacket()); // ask neighbor for his count
            }

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

        private void dispatcher_QueryPeerCountPacketArrived(QueryPeerCountPacket packet, Peer peer)
        {
            network.SendPacket(new PeerCountPacket() { Count = peerCount }, peer);
        }

        private void network_DocumentPacketArrived(DocumentPacket packet, Peer peer)
        {
            lock (documentText) {
                documentText.Clear();
                documentText.Append(packet.Document);
            }
            FireTextChanged();
        }

        private void dispatcher_PeerCountPacketArrived(PeerCountPacket packet, Peer peer)
        {
            peerCount = packet.Count;
            FirePeerCountChanged();
        }

        private void network_UpdateCaretPackageArrived(UpdateCaretPacket packet, Peer peer)
        {
            FireCaretPositionChanged(peer, packet.Position);
        }

        void dispatcher_NewPeerPacketArrived(NewPeerPacket packet, Peer peer)
        {
            peerCount++;
            FirePeerCountChanged();
        }

        void dispatcher_LostPeerPacketArrived(LostPeerPacket packet, Peer peer)
        {
            peerCount--;
            FirePeerCountChanged();
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

        protected void FirePeerCountChanged()
        {
            var handler = PeerCountChanged;
            if (handler != null)
                handler(this, new PeerCountChangedEventArgs(peerCount));
        }
    }
}