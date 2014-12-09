using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [DataContract]
    public class AddTextPacket
    {
        [DataMember]
        public int Offset { get; set; }

        [DataMember]
        public string Text { get; set; }
    }
}
