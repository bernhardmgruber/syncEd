using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Packets
{
	public class DocumentPacket : Packet
	{
		public string Document { get; set; }
	}
}
