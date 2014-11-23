using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network
{
	class Peer
	{
		public TcpClient Tcp { get; set; }

		public Peer(TcpClient tcp)
		{
			Tcp = tcp;
		}
	}
}
