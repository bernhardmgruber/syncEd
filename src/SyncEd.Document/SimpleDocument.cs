using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace SyncEd.Document
{
    public class SimpleDocument
        : IDocument
    {
        public bool IsConnected { get; private set; }
        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            bool exists = documentName == "Test";
            IsConnected = exists;
            return Task.Delay(500).ContinueWith(task => exists);
        }

        public Task Close()
        {
            bool wasConnected = IsConnected;

            IsConnected = false;

            return Task.Delay(wasConnected ? 250 : 0);
        }
    }
}