using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace SyncEd.Network.Tcp
{
	public delegate void NewLinkHandler(TcpLink p);
	public delegate void OwnIPDetectedHandler(IPEndPoint a);

	// @see: http://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx
	internal class TcpLinkEstablisher
	{
		internal event NewLinkHandler NewLinkEstablished;
		internal event OwnIPDetectedHandler OwnIPDetected;

		private const char documentPortSeparator = '\0';

		private const int broadcastPort = 1337; // UDP port for sending broadcasts
		private const int linkEstablishTimeoutMs = 1000;

		private int tcpListenPort = 1338;    // first tried TCP port for listening after broadcasts

		private UdpClient udp;
		private Thread udpListenThread;

		private TcpListener tcpListener;

		private string documentName;

		internal TcpLinkEstablisher(string documentName)
		{
			this.documentName = documentName;
			StartListeningForPeers();
		}

		private byte[] ToBytes(string str)
		{
			return Encoding.Unicode.GetBytes(str);
		}

		private string ToString(byte[] bytes)
		{
			return Encoding.Unicode.GetString(bytes);
		}

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		internal bool FindPeer()
		{
			Debug.Assert(!tcpListener.Pending());
			var peerTask = tcpListener.AcceptTcpClientAsync();

			// send a broadcast with the document name into the network
			Console.WriteLine("Broadcasting for " + documentName);
			udp.Client.SendTo(ToBytes(documentName + documentPortSeparator + tcpListenPort), new IPEndPoint(IPAddress.Broadcast, broadcastPort));

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

		private bool IsLocalAddress(IPAddress address)
		{
			return Dns.GetHostAddresses(Dns.GetHostName()).Any(a => a.Equals(address));
		}

		private void FireNewLinkEstablished(TcpLink tcp)
		{
			// copy handler reference for thread safety
			var handler = NewLinkEstablished;
			if (handler != null)
				handler(tcp);
		}

		private void FireOwnIPDetected(IPEndPoint address)
		{
			var handler = OwnIPDetected;
			if (handler != null)
				handler(address);
		}

		private TcpListener FindTcpListener()
		{
			// find and open TCP listening port for incoming connection
			while(true)
			{
				try
				{
					var l = new TcpListener(IPAddress.Any, tcpListenPort);
					l.Start(1); // only listen for 1 connection
					Console.WriteLine("Started TCP listener on port " + tcpListenPort);
					return l;
				}
				catch (SocketException)
				{
					//Console.WriteLine("Failed to establish TCP listener on port " + tcpListenPort + ": " + e);
					tcpListenPort++;
					if(tcpListenPort >= 0xFFFF)
						throw new Exception("Failed to find a TCP port for listening");
					continue;
				}
			}
		}

		/// <summary>
		/// Listens on the network for new peers with the given document name.
		/// If such a peer connects, the NewLinkEstablished event is fired.
		/// </summary>
		/// <param name="documentName"></param>
		private void StartListeningForPeers()
		{
			tcpListener = FindTcpListener();

			udp = new UdpClient();
			udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			udp.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
			udp.EnableBroadcast = true;

			udpListenThread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						Console.WriteLine("Waiting for broadcast");
						var ep = new IPEndPoint(IPAddress.Any, broadcastPort);
						byte[] bytes;
						try
						{
							bytes = udp.Receive(ref ep);
						}
						catch (Exception)
						{
							// asume the socket has been closed for shutting down
							return;
						}
						if (bytes != null && bytes.Length != 0)
						{
							var parts = ToString(bytes).Split(documentPortSeparator);
							Debug.Assert(parts.Length == 2);
							string peerDocumentName = parts[0];
							int remoteTcpListenPort = int.Parse(parts[1]);
							Console.WriteLine("Received broadcast from {0}: {1}", ep.Address, peerDocumentName);

							if (IsLocalAddress(ep.Address) && remoteTcpListenPort == tcpListenPort)
							{
								Console.WriteLine("Self broadcast detected");
								FireOwnIPDetected(new IPEndPoint(ep.Address, remoteTcpListenPort));
							}
							else if (peerDocumentName != documentName)
								Console.WriteLine("Mismatch in document name");
							else
							{
								// establish connection to peer
								var tcp = new TcpClient();
								Console.WriteLine("TCP connect to " + ep.Address + ":" + remoteTcpListenPort);
								try
								{
									tcp.Connect(ep.Address, remoteTcpListenPort);
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
			});

			udpListenThread.Start();
		}


		internal void Close()
		{
			tcpListener.Stop();
			udp.Close();
			udpListenThread.Join();
		}
	}
}
