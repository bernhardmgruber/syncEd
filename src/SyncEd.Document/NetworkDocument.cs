using System;
using System.Threading.Tasks;
using SyncEd.Network;

namespace SyncEd.Document
{
    public class NetworkDocument
        : IDocument
    {
        private readonly INetwork network;

        public NetworkDocument(INetwork network)
        {
            this.network = network;
        }

        public bool IsConnected { get; private set; }

        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            return Task.Run(() => {
                // TODO
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