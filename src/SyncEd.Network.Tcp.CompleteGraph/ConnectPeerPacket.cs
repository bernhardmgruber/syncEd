using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp.CompleteGraph
{
	[Serializable]
	internal class ExpectNewPeerPacket
	{
		internal Peer Peer { get; set; }
	}
}
