using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	public class PeerObject
	{
		public Peer Peer { get; set; }
		public object Object { get; set; }

		public override string ToString()
		{
			return "PeerObject {" + Peer + ", " + Object + "}";
		}
	}

	[Serializable]
	public class UdpPacket
	{
		public string DocumentName { get; set; }
	}

	[Serializable]
	public class FindPacket : UdpPacket
	{
		public int ListenPort { get; set; }
	}

	[Serializable]
	public class PeerDiedPacket : UdpPacket
	{
		public Peer DeadPeer { get; set; }
		public Peer RepairPeer { get; set; }
	}
}
