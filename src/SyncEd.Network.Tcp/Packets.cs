using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	internal class PeerObject
	{
		internal Peer Peer { get; set; }
		internal object Object { get; set; }

		public override string ToString()
		{
			return "PeerObject {" + Peer + ", " + Object + "}";
		}
	}

	[Serializable]
	internal class UdpPacket
	{
		internal string DocumentName { get; set; }
	}

	[Serializable]
	internal class FindPacket : UdpPacket
	{
		internal int ListenPort { get; set; }
	}

	[Serializable]
	internal class PeerDiedPacket : UdpPacket
	{
		internal Peer DeadPeer { get; set; }
		internal Peer RepairPeer { get; set; }
	}
}
