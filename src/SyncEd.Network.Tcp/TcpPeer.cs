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
	public delegate void FailedHandler(TcpPeer sender);

	public class TcpPeer
	{
		public event ObjectReceivedHandler ObjectReceived;
		public event FailedHandler Failed;

		public Peer Peer { get; private set; }

		private TcpClient Tcp { get; set; }

		private Thread sendThread;
		private Thread recvThread;

		private BlockingCollection<object> sendColl = new BlockingCollection<object>();

		private CancellationTokenSource cancelSource;

		public TcpPeer(TcpClient tcp)
		{
			Tcp = tcp;
			Peer = new Peer() { Address = (tcp.Client.RemoteEndPoint as IPEndPoint).Address };

			cancelSource = new CancellationTokenSource();
			var token = cancelSource.Token;

			sendThread = new Thread(new ThreadStart(() =>
			{
				while (!token.IsCancellationRequested)
				{
					try
					{
						var o = sendColl.Take(token);
						var f = new BinaryFormatter();
						f.Serialize(Tcp.GetStream(), o);
					}
					catch (Exception e)
					{
						Console.WriteLine("Send in " + ToString() + " failed: " + e);
						FireFailed();
					}
				}
			}));
			sendThread.Start();

			recvThread = new Thread(() =>
			{
				var f = new BinaryFormatter();
				while (!token.IsCancellationRequested)
				{
					try
					{
						FireObjectReceived(f.Deserialize(Tcp.GetStream()));
					}
					catch (Exception e)
					{
						Console.WriteLine("Receive in " + ToString() + " failed: " + e);
						FireFailed();
					}
				}
			});
			recvThread.Start();
		}

		private void FireObjectReceived(object o)
		{
			var handler = ObjectReceived;
			if (handler == null)
				Console.Write("FATAL: No packet handler on TCP Peer " + ToString() + ". Packat lost.");
			else
				handler(o, Peer);
		}

		private void FireFailed()
		{
			var handler = Failed;
			if (handler == null)
				Console.Write("FATAL: No fail handler on TCP Peer " + ToString());
			else
				handler(this);
		}

		public void SendAsync(object o)
		{
			sendColl.Add(o);
		}

		public void Close()
		{
			cancelSource.Cancel();
			Tcp.GetStream().Close();
			sendThread.Join();
			recvThread.Join();
			Tcp.Close();
		}

		public override string ToString()
		{
			return "TcpPeer {" + (Tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
		}
	}
}
