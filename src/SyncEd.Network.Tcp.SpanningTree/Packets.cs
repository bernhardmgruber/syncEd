using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{


	[Serializable]
	public class PeerDiedPacket
	{
		public Peer DeadPeer { get; set; }
		public Peer RepairPeer { get; set; }
	}
}
