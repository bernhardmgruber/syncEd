using System;
using SyncEd.Network;

namespace SyncEd.Document
{
    public class CaretChangedEventArgs
        : EventArgs
    {
        public Peer Peer { get; private set; }

        public int? Position { get; set; }

        public CaretChangedEventArgs(Peer peer, int? position)
        {
            Peer = peer;
            Position = position;
        }
    }
}