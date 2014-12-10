using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [DataContract]
    public class DocumentPacket
    {
        [DataMember]
        public string Document { get; set; }
    }
}
