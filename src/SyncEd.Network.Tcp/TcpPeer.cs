using System.Net;
using System.Net.Sockets;

namespace SyncEd.Network.Tcp
{
    public class TcpPeer
    {
        public Peer Peer { get; private set; }

        public TcpClient Tcp { get; set; }

        public TcpPeer(TcpClient tcp)
        {
            Tcp = tcp;
            Peer = new Peer() { Address = (tcp.Client.RemoteEndPoint as IPEndPoint).Address };
        }

        public override string ToString()
        {
            return "TcpPeer { " + (Tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
        }
    }
}
