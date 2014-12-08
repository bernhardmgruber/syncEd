using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Packets
{
    [DataContract]
    public class DeleteTextPacket
    {
        public int Offset { get; set; }
        public int Length { get; set; }
    }
}
