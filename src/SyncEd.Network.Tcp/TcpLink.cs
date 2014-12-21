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
	internal delegate void ObjectReceivedHandler(TcpLink sender, object o);
	internal delegate void FailedHandler(TcpLink sender, byte[] failedData);

	public class TcpLink
	{
		internal event ObjectReceivedHandler ObjectReceived;
		internal event FailedHandler Failed;

		internal Peer Peer { get; private set; }

		private TcpClient tcp;
		private NetworkStream stream;
		private Thread recvThread;
		private volatile bool closed = false;

		internal TcpLink(TcpClient tcp, Peer peer)
		{
			this.tcp = tcp;
			this.Peer = peer;
			stream = tcp.GetStream();
		}

		internal void Start()
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
					// the fail handler has to run on another thread than the recv thread as this thread has to be shut down
					Task.Run(() => FireFailed());
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
			handler(this, failedData);
		}

		internal void Send(byte[] bytes)
		{
			if (closed)
				throw new ObjectDisposedException("TcpLink has already been closed");

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

		internal void Close()
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
