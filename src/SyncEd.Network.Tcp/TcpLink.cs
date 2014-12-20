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
	public delegate void ObjectReceivedHandler(TcpLink sender, object o);
	public delegate void FailedHandler(TcpLink sender, byte[] failedData);

	public class TcpLink
	{
		public event ObjectReceivedHandler ObjectReceived;
		public event FailedHandler Failed;

		public Peer Peer { get; private set; }

		private TcpClient tcp;
		private NetworkStream stream;
		private Thread recvThread;
		private bool closed = false;

		public TcpLink(TcpClient tcp, Peer peer)
		{
			this.tcp = tcp;
			this.Peer = peer;
			stream = tcp.GetStream();
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
			handler(this, o);
		}

		private void FireFailed(byte[] failedData = null)
		{
			var handler = Failed;
			Debug.Assert(handler != null, "FATAL: No fail handler on TCP Peer " + this);
			Task.Run(() => handler(this, failedData)); // when failing, the fail handler have to run on another thread than the threads used by this TcpLink as these threads have to be shut down
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
				FireFailed(bytes);
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
			return "TcpLink {" + (tcp.Client.RemoteEndPoint as IPEndPoint) + "}";
		}
	}
}
