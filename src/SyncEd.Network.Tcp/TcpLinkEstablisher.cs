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
        const int broadcastPort = 1337; // UDP port for sending broadcasts
        const int listenPort = 1338;    // TCP port for listening after broadcasts
        const int linkEstablishTimeoutMs = 3000;

        public event NewLinkHandler NewLinkEstablished;

        private Thread listenThread;
        private CancellationTokenSource cancelSource;

        private string documentName;

        public TcpLinkEstablisher(string documentName)
        {
            this.documentName = documentName;
            StartListeningForPeers();
        }

        /// <summary>
        /// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
        /// </summary>
        public TcpPeer FindPeer()
        {
            // open listening port for incoming connection
            var listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start(1); // only listen for 1 connection
            var peerTask = listener.AcceptTcpClientAsync();

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
                var tcpIn = peerTask.Result;
                var ep = (IPEndPoint)tcpIn.Client.RemoteEndPoint;
                Console.WriteLine("Answer from " + ep.Address);

                Console.WriteLine("Establishing duplex link");
                var tcpOut = new TcpClient();
                try
                {
                    tcpOut.Connect(ep.Address, listenPort);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to connet: " + e);
                    tcpOut.Close();
                }
                Console.WriteLine("Connection established");

                peer = new TcpPeer(tcpIn, tcpOut);
            }
            else
                Console.WriteLine("No answer. I'm first owner");

            // stop listening
            listener.Stop();

            return peer;
        }

        /// <summary>
        /// Listens on the network for new peers with the given document name.
        /// If such a peer connects, the NewLinkEstablished event is fired.
        /// </summary>
        /// <param name="documentName"></param>
        void StartListeningForPeers()
        {
            cancelSource = new CancellationTokenSource();
            var token = cancelSource.Token;

            listenThread = new Thread(() => {
                using (var udpClient = new UdpClient(broadcastPort)) {
                    udpClient.EnableBroadcast = true;
                    token.Register(() => udpClient.Close()); // causes Receive() to return
                    while (!token.IsCancellationRequested) {
                        try {
                            Console.WriteLine("Waiting for broadcast");
                            var ep = new IPEndPoint(IPAddress.Any, broadcastPort);
                            byte[] bytes = udpClient.Receive(ref ep);
                            if (bytes != null && bytes.Length != 0) {
                                string peerDocumentName = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                                Console.WriteLine("Received broadcast from {0}: {1}", ep.Address, peerDocumentName);

                                if (peerDocumentName == documentName) {
                                    // create listener for duplex link
                                    var listener = new TcpListener(IPAddress.Any, listenPort);
                                    listener.Start(1);
                                    var peerTask = listener.AcceptTcpClientAsync();

                                    // establish connection to peer
                                    var tcpOut = new TcpClient();
                                    Console.WriteLine("TCP connect to " + ep.Address);
                                    try
                                    {
                                        tcpOut.Connect(ep.Address, listenPort);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Failed to connet: " + e);
                                        tcpOut.Close();
                                    }
                                    Console.WriteLine("Waiting for duplex link");

                                    if (peerTask.Wait(linkEstablishTimeoutMs))
                                    {
                                        var tcpIn = peerTask.Result;
                                        Console.WriteLine("Connection established");

                                        if (NewLinkEstablished != null) // Warning: not thread safe
                                            NewLinkEstablished(new TcpPeer(tcpIn, tcpOut));
                                    }
                                    else
                                        tcpOut.Close();

                                    listener.Stop();
                                }
                            }
                        } catch (Exception e) {
                            Console.WriteLine("Exception in UDP broadcast listening: " + e.ToString());
                        }
                    }
                }
            });

            listenThread.Start();
        }

        public void Close()
        {
            cancelSource.Cancel();
            listenThread.Join();
        }
    }
}
