using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public class TcpBroadcastNetwork : IDisposable
	{
		private TcpListener listener;

		private const int linkEstablishTimeoutMs = 500;
		private const int linkHandshakeTimeoutMs = 200;

		//private Action<TcpClient, Peer> newLinkEstablished;
		private Action<TcpLink, byte[]> linkFailed;
		private Action<TcpLink, object> objectReceived;

		private int tcpListenPort = 1338;    // first tried TCP port for listening after broadcasts
		public int ListenPort { get { return tcpListenPort; } }

		public List<TcpLink> Links { get; private set; }

		public TcpBroadcastNetwork(Action<TcpLink, object> objectReceived, Action<TcpLink, byte[]> linkFailed)
		{
			//this.newLinkEstablished = newLinkEstablished;
			this.linkFailed = linkFailed;
			this.objectReceived = objectReceived;
			Links = new List<TcpLink>();

			FindTcpListener();
		}

		private void FindTcpListener()
		{
			// find and open TCP listening port for incoming connection
			listener = null;
			for (; tcpListenPort < 0xFFFF; tcpListenPort++)
			{
				try
				{
					listener = new TcpListener(IPAddress.Any, tcpListenPort);
					listener.ExclusiveAddressUse = true;
					listener.Start();
					break;
				}
				catch (Exception)
				{
					continue;
				}
			}
			if (listener == null)
				throw new Exception("Failed to find a TCP port for listening");

			Console.WriteLine("Bound TCP listener to port " + tcpListenPort);
		}

		public Peer EstablishConnectionTo(IPEndPoint peerEP)
		{
			lock (Links)
				if (Links.Find(l => l.Peer.Equals(peerEP)) != null)
				{
					Console.WriteLine("Tried to connet to peer twice.");
					return null;
				}

			var tcp = new TcpClient();
			Console.WriteLine("TCP connect to " + peerEP + ": ");
			try
			{
				tcp.Connect(peerEP);
				tcp.ReceiveTimeout = linkHandshakeTimeoutMs;

				// send own port
				tcp.GetStream().Write(BitConverter.GetBytes(tcpListenPort), 0, sizeof(int));

				// receive remote port
				byte[] bytes = new byte[sizeof(int)];
				Debug.Assert(tcp.GetStream().Read(bytes, 0, sizeof(int)) == sizeof(int), "sizeof(int) in Establish");
				int remotePort = BitConverter.ToInt32(bytes, 0);
				if (peerEP.Port != remotePort)
					throw new Exception("Port mismatch during handshake");

				tcp.ReceiveTimeout = 0;
			}
			catch (Exception)
			{
				Console.WriteLine("Connect failed. Timeout?");
				tcp.Close();
				return null;
			}

			Console.WriteLine("ESTABLISHED");
			var peer = new Peer() { EndPoint = peerEP };
			NewLinkEstablished(tcp, peer);
			return peer;
		}

		/// <summary>
		/// Returns true if a peer has successfully connected (handshake ok and no timeout)
		/// </summary>
		/// <returns></returns>
		public bool WaitForTcpConnect()
		{
			var peerTask = listener.AcceptTcpClientAsync();
			Console.WriteLine("Waiting for TCP connect");
			if (peerTask.Wait(linkEstablishTimeoutMs))
			{
				var tcp = peerTask.Result;

				// receive peer's port
				byte[] portBytes = new byte[sizeof(int)];
				Debug.Assert(tcp.GetStream().Read(portBytes, 0, sizeof(int)) == sizeof(int), "sizeof(int) in WaitForConnect"); // assume an int gets sent at once
				int remotePort = BitConverter.ToInt32(portBytes, 0);

				var address = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address;
				var peerEp = new IPEndPoint(address, remotePort);
				var peer = new Peer() { EndPoint = peerEp };

				lock (Links)
					if (Links.Find(l => l.Peer.Equals(peer)) != null)
					{
						Console.WriteLine("Warning: Peer " + peer + " connected twice.");
						tcp.Close();
						return false;
					}

				// send own port
				tcp.GetStream().Write(BitConverter.GetBytes(tcpListenPort), 0, sizeof(int));

				Console.WriteLine("TCP connect from " + peerEp + ". ESTABLISHED");
				//Console.WriteLine("Connection established");
				NewLinkEstablished(tcp, peer);
				return true;
			}
			else
			{
				Console.WriteLine("Timeout");
				return false;
			}
		}

		private void NewLinkEstablished(TcpClient tcp, Peer peer)
		{
			TcpLink l = null;
			lock (Links)
			{
				Debug.Assert(Links.Find(t => t.Peer.Equals(peer)) == null, "Peer " + peer + "is already connected");
				l = new TcpLink(tcp, peer, objectReceived, linkFailed);
				Links.Add(l);
			}
		}

		public void MulticastObject(object o, Predicate<TcpLink> pred)
		{
			Console.WriteLine("TCP out (" + Links.Count + "): " + o);
			byte[] data = Utils.Serialize(o);
			lock (Links)
				foreach (TcpLink l in Links)
					if (pred(l))
						l.Send(data);
		}

		public void BroadcastObject(object o)
		{
			MulticastObject(o, l => true);
		}

		public void Dispose()
		{
			lock (Links)
				Links.ForEach(p => p.Dispose());
			Links = new List<TcpLink>();
			listener.Stop();
		}
	}
}
