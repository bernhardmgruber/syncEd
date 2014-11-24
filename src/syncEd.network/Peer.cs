using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network
{
	public class Peer
	{
		public TcpClient Tcp { get; set; }

		public Peer(TcpClient tcp)
		{
			Tcp = tcp;
		}

		public override string ToString()
		{
			return "Peer { " + (Tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
		}
	}
}
