using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public delegate void ObjectReceivedHandler(object o, Peer p);
	public delegate void FailedHandler(TcpLink sender);

	public class TcpLink
	{
		public event ObjectReceivedHandler ObjectReceived;
		public event FailedHandler Failed;

		public Peer Peer { get; private set; }

		private TcpClient tcp;
		private NetworkStream stream;
		private Thread recvThread;
		private bool closed = false;

		public TcpLink(TcpClient tcp)
		{
			this.tcp = tcp;
			this.stream = tcp.GetStream();
			Peer = new Peer() { Address = (tcp.Client.RemoteEndPoint as IPEndPoint).Address };
		}

		public void Start()
		{
			recvThread = new Thread(() =>
			{
				var f = new BinaryFormatter();
				try
				{
					while (true)
						FireObjectReceived(f.Deserialize(stream));
				}
				catch (Exception)
				{
					//Console.WriteLine("Receive in " + ToString() + " failed: " + e);
					FireFailed();
				}
			});
			recvThread.Start();
		}

		private void FireObjectReceived(object o)
		{
			var handler = ObjectReceived;
			Debug.Assert(handler != null, "FATAL: No packet handler on TCP Peer " + this + ". Packet lost.");
			handler(o, Peer);
		}

		private void FireFailed()
		{
			var handler = Failed;
			Debug.Assert(handler != null, "FATAL: No fail handler on TCP Peer " + this);
			Task.Run(() => handler(this)); // when failing, the fail handler have to run on another thread than the threads used by this TcpLink as these threads have to be shut down
		}

		public void Send(byte[] bytes)
		{
			try
			{
				stream.WriteAsync(bytes, 0, bytes.Length);
			}
			catch (Exception e)
			{
				Console.WriteLine("Send in " + this + " failed: " + e);
				FireFailed();
			}
		}

		public void Close()
		{
			if (!closed)
			{
				Console.WriteLine("Closing Link " + this);
				closed = true;
				stream.Dispose(); // closes socket and causes receiver thread to FireFailed() if it is still running
				recvThread.Join();
				tcp.Close();
			}
		}

		public override string ToString()
		{
			if (!closed)
				return "TcpPeer {" + (tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
			else
				return "TcpPeer {dead}";
		}
	}
}
