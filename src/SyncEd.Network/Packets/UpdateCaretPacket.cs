using System;
using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [Serializable]
    [DataContract]
    [AutoForward]
    public class UpdateCaretPacket
    {
        [DataMember]
        public int? Position { get; set; }
    }
}
