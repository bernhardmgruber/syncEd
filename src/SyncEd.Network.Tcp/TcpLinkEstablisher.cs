using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SyncEd.Network.Tcp
{
	public delegate void NewLinkHandler(TcpLink p);

	// @see: http://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx
	public class TcpLinkEstablisher
	{
		const int broadcastPort = 1337; // UDP port for sending broadcasts
		const int listenPort = 1338;    // TCP port for listening after broadcasts
		const int linkEstablishTimeoutMs = 1000;

		public event NewLinkHandler NewLinkEstablished;

		private Thread udpListenThread;

		private CancellationTokenSource cancelSource;

		private string documentName;

		public TcpLinkEstablisher(string documentName)
		{
			this.documentName = documentName;
			StartListeningForPeers();
		}

		private byte[] toBytes(string str)
		{
			return Encoding.Unicode.GetBytes(str);
		}

		private string toString(byte[] bytes)
		{
			return Encoding.Unicode.GetString(bytes);
		}

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		public bool FindPeer()
		{
			TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
			try
			{
				// open listening port for incoming connection
				listener.Start(1); // only listen for 1 connection
				var peerTask = listener.AcceptTcpClientAsync();

				// send a broadcast with the document name into the network
				Console.WriteLine("Broadcasting for " + documentName);
				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { EnableBroadcast = true })
				{
					IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
					s.SendTo(toBytes(documentName), ep);
				}

				// wait for an answer
				Console.WriteLine("Waiting for TCP connect");
				if (peerTask.Wait(linkEstablishTimeoutMs))
				{
					var tcp = peerTask.Result;
					Console.WriteLine("TCP connect from " + ((IPEndPoint)tcp.Client.RemoteEndPoint).Address);
					Console.WriteLine("Connection established");
					FireNewLinkEstablished(new TcpLink(tcp));
					return true;
				}
				else
				{
					Console.WriteLine("No answer. I'm first owner");
					return false;
				}
			}
			finally
			{
				// stop listening
				listener.Stop();
			}
		}

		bool IsLocalAddress(IPAddress address)
		{
			return Dns.GetHostAddresses(Dns.GetHostName()).Any(a => a.Equals(address));
		}

		void FireNewLinkEstablished(TcpLink tcp)
		{
			// copy handler reference for thread safety
			var handler = NewLinkEstablished;
			if (handler != null)
				handler(tcp);
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

			udpListenThread = new Thread(() =>
			{
				using (var udpClient = new UdpClient(broadcastPort))
				{
					udpClient.EnableBroadcast = true;
					token.Register(() => udpClient.Close()); // causes Receive() to return
					while (!token.IsCancellationRequested)
					{
						try
						{
							Console.WriteLine("Waiting for broadcast");
							var ep = new IPEndPoint(IPAddress.Any, broadcastPort);
							byte[] bytes = udpClient.Receive(ref ep);
							if (bytes != null && bytes.Length != 0)
							{
								string peerDocumentName = toString(bytes);
								Console.WriteLine("Received broadcast from {0}: {1}", ep.Address, peerDocumentName);

								if (IsLocalAddress(ep.Address))
									Console.WriteLine("Self broadcast detected");
								else if (peerDocumentName != documentName)
									Console.WriteLine("Mismatch in document name");
								else
								{
									// establish connection to peer
									var tcp = new TcpClient();
									Console.WriteLine("TCP connect to " + ep.Address);
									try
									{
										tcp.Connect(ep.Address, listenPort);
									}
									catch (Exception e)
									{
										Console.WriteLine("Failed to connet: " + e);
										tcp.Close();
									}

									Console.WriteLine("Connection established");
									FireNewLinkEstablished(new TcpLink(tcp));
								}
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("Exception in UDP broadcast listening: " + e.ToString());
						}
					}
				}
			});

			udpListenThread.Start();
		}

		public void Close()
		{
			cancelSource.Cancel();
			udpListenThread.Join();
		}
	}
}
