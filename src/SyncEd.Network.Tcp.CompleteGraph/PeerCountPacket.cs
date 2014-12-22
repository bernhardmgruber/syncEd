using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp.CompleteGraph
{
	[Serializable]
	internal class PeersToConnectPacket
	{
		internal Peer[] Peers { get; set; }
	}
}
