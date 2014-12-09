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
        const int BroadcastPort = 1337;

        // TCP port for listening after broadcasts
        const int ListenPort = 1338;

        const int LinkEstablishTimeoutMs = 1000;

        public event NewLinkHandler NewLinkEstablished;

        /// <summary>
        /// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
        /// </summary>
        public TcpPeer FindPeer(string documentName)
        {
            // open listening port for incoming connection
            var haveListener = new TcpListener(IPAddress.Any, ListenPort);
            haveListener.Start(1); // only listen for 1 connection
            var peerTask = haveListener.AcceptTcpClientAsync();

            // send a broadcast with the document name into the network
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
                s.EnableBroadcast = true;
                IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);


                byte[] bytes = Encoding.ASCII.GetBytes(documentName);
                s.SendTo(bytes, ep);
            }

            // wait for an answer
            TcpPeer peer = null;
            if (peerTask.Wait(LinkEstablishTimeoutMs))
                peer = new TcpPeer(peerTask.Result);

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
                using (var udpClient = new UdpClient(BroadcastPort)) {
                    udpClient.Client.ReceiveTimeout = 1000;

                    while (!token.IsCancellationRequested) {
                        try {
                            Console.WriteLine("Waiting for broadcast");
                            var clientEP = new IPEndPoint(IPAddress.Any, BroadcastPort);
                            byte[] bytes = udpClient.Receive(ref clientEP);
                            string peerDocumentName = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                            Console.WriteLine("Received broadcast from {0}:\n {1}\n", clientEP.ToString(), peerDocumentName);

                            if (peerDocumentName == documentName) {
                                // establish connection to peer
                                var client = new TcpClient();
                                client.Connect(clientEP.Address, ListenPort);

                                if (NewLinkEstablished != null) // Warning: not thread safe
                                    NewLinkEstablished(new TcpPeer(client));
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
