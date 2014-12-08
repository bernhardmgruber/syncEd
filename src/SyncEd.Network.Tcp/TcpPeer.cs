using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public class TcpPeer
	{
        public Peer Peer { get; private set; }

		public TcpClient Tcp { get; set; }

		public TcpPeer(TcpClient tcp)
		{
			Tcp = tcp;
            Peer = new Peer() { IP = (tcp.Client.RemoteEndPoint as IPEndPoint).Address };
		}

		public override string ToString()
		{
            return "TcpPeer { " + (Tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
		}
	}
}
