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
	public class TcpLink : IDisposable
	{
		public Peer Peer { get; private set; }

		private TcpClient tcp;
		private NetworkStream stream;
		private Thread recvThread;
		private volatile bool disposed = false;

		private Action<TcpLink, object> objectReceived;
		private Action<TcpLink, byte[]> failed;

		public TcpLink(TcpClient tcp, Peer peer, Action<TcpLink, object> objectReceived, Action<TcpLink, byte[]> failed)
		{
			this.tcp = tcp;
			this.Peer = peer;
			this.objectReceived = objectReceived;
			this.failed = failed;
			stream = tcp.GetStream();

			Start();
		}

		private void Start()
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
					//Console.WriteLine("Receive in " + this + " failed: " + e);
					Task.Run(() => FireFailed());
				}
			});
			recvThread.Start();
		}

		private void FireObjectReceived(object o)
		{
			objectReceived(this, o);
		}

		private void FireFailed(byte[] failedData = null)
		{
			failed(this, failedData);
		}

		public void Send(byte[] bytes)
		{
			if (disposed)
				throw new ObjectDisposedException("TcpLink has already been closed");

			try
			{
				stream.WriteAsync(bytes, 0, bytes.Length);
			}
			catch (Exception)
			{
				FireFailed(bytes);
			}
		}

		public void Dispose()
		{
			if (!disposed)
			{
				Console.WriteLine("Closing Link " + this);
				disposed = true;
				stream.Dispose(); // closes socket and causes receiver thread to FireFailed() if it is still running
				recvThread.Join();
				tcp.Close();
			}
		}

		public override string ToString()
		{
			if (disposed)
				return "TcpLink {disposed}";
			else
				return "TcpLink {" + Peer + "}";
		}
	}
}
