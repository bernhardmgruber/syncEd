using System;
using System.Threading.Tasks;
using SyncEd.Document;

namespace SyncEd.Network
{
    public class NetworkDocument
        : IDocument
    {
        private readonly LinkEstablisher linkEstablisher;

        public NetworkDocument(LinkEstablisher linkEstablisher)
        {
            this.linkEstablisher = linkEstablisher;
        }

        public bool IsConnected { get; private set; }

        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            return Task.Run(() => {
                var peer = linkEstablisher.FindPeer(documentName);
                if (peer != null) {
                    // TODO
                } else {
                    // TODO
                }
                return true;
            });
        }

        public Task Close()
        {
            throw new NotImplementedException();
        }

        public void ChangeText(int offset, int length, string text)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;
    }
}