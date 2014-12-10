using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
    public delegate void NewLinkHandler(TcpPeer p);

    // @see: http://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx
    public class TcpLinkEstablisher
    {
        // UDP port for sending broadcasts
        const int broadcastPort = 1337;

        // TCP port for listening after broadcasts
        const int listenPort = 1338;

        const int linkEstablishTimeoutMs = 3000;

        public event NewLinkHandler NewLinkEstablished;

        /// <summary>
        /// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
        /// </summary>
        public TcpPeer FindPeer(string documentName)
        {
            // open listening port for incoming connection
            var haveListener = new TcpListener(IPAddress.Any, listenPort);
            haveListener.Start(10); // only listen for 1 connection
            var peerTask = haveListener.AcceptTcpClientAsync();

            // send a broadcast with the document name into the network
            Console.WriteLine("Broadcasting for " + documentName);
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
                s.EnableBroadcast = true;
                IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

                byte[] bytes = Encoding.ASCII.GetBytes(documentName);
                s.SendTo(bytes, ep);
            }

            // wait for an answer
            Console.WriteLine("Waiting for answer");
            TcpPeer peer = null;
            if (peerTask.Wait(linkEstablishTimeoutMs))
            {
                peer = new TcpPeer(peerTask.Result);
                Console.WriteLine("Answer from " + peer.Peer.Address);
            }
            else
                Console.WriteLine("No answer. I'm first owner");

            // stop listening
            haveListener.Stop();

            return peer;
        }

        /// <summary>
        /// Listens on the network for new peers with the given document name.
        /// If such a peer connects, the NewLinkEstablished event is fired.
        /// </summary>
        /// <param name="documentName"></param>
        public void ListenForPeers(string documentName, CancellationToken token)
        {
            Task.Run(() => {
                using (var udpClient = new UdpClient(broadcastPort)) {
                    udpClient.EnableBroadcast = true;
                    token.Register(() => udpClient.Close()); // causes Receive() to return
                    while (!token.IsCancellationRequested) {
                        try {
                            Console.WriteLine("Waiting for broadcast");
                            var clientEP = new IPEndPoint(IPAddress.Any, broadcastPort);
                            byte[] bytes = udpClient.Receive(ref clientEP);
                            if (bytes != null && bytes.Length != 0) {
                                string peerDocumentName = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                                Console.WriteLine("Received broadcast from {0}: {1}", clientEP.Address, peerDocumentName);

                                if (peerDocumentName == documentName) {
                                    // establish connection to peer
                                    var client = new TcpClient();
                                    Console.WriteLine("TCP connect to " + clientEP.Address);
                                    client.Connect(clientEP.Address, listenPort);
                                    Console.WriteLine("TCP connect success");
                                    if (NewLinkEstablished != null) // Warning: not thread safe
                                        NewLinkEstablished(new TcpPeer(client));
                                }
                            }
                        } catch (Exception e) {
                            Console.WriteLine("Exception in UDP broadcast listening: " + e.ToString());
                        }
                    }
                }
            }, token);
        }
    }
}
