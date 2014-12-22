using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	internal class PeerDiedPacket
	{
		internal Peer DeadPeer { get; set; }
		internal Peer RepairPeer { get; set; }
	}
}
