using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [DataContract]
    public class DeleteTextPacket
    {
        [DataMember]
        public int Offset { get; set; }

        [DataMember]
        public int Length { get; set; }
    }
}
