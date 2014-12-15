using System;
using System.Net;

namespace SyncEd.Network
{
    [Serializable]
    public class Peer
    {
        public IPAddress Address { get; set; }
    }
}
