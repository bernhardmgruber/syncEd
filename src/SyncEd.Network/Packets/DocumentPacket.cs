using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [DataContract]
    public class DocumentPacket
    {
        public string Document { get; set; }
    }
}
