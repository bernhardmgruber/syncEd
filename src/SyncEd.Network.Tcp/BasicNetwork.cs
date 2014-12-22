using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public abstract class BasicNetwork : INetwork
	{
		public event PacketHandler PacketArrived;
		public Peer Self { get; protected set; }

		private ManualResetEvent ownIPWaitHandle;

		protected string documentName;
		protected UdpBroadcastNetwork udpNetwork;
		protected TcpBroadcastNetwork tcpNetwork;

		protected abstract void ProcessCustomTcpObject(TcpLink link, TcpObject o);
		protected abstract void ProcessCustomUdpObject(IPEndPoint endpoint, UdpObject o);
		protected abstract void PeerFailed(TcpLink link, byte[] failedData);

		public virtual bool Start(string documentName)
		{
			this.documentName = documentName;

			ownIPWaitHandle = new ManualResetEvent(false);
			udpNetwork = new UdpBroadcastNetwork((o, e) => ProcessUdpObject(o, e));
			tcpNetwork = new TcpBroadcastNetwork((l, o) => ProcessTcpObject(l, o), (l, d) => PeerFailed(l, d));

			var found = FindPeer();
			ownIPWaitHandle.WaitOne(); // wait for listener thread to receive self broadcast and determine own IP
			return found;
		}

		public virtual void Stop()
		{
			udpNetwork.Stop();
			tcpNetwork.Dispose();
			ownIPWaitHandle.Dispose();
		}

		public virtual void SendPacket(object packet)
		{
			Debug.Assert(Self != null, "Own IP has not been determined for send");
		}

		protected void FirePacketArrived(object packet, Peer peer, SendBackFunc sendBack)
		{
			var handler = PacketArrived;
			if (handler != null)
				handler(packet, peer, sendBack);
		}

		private void ProcessUdpObject(object o, IPEndPoint endpoint)
		{
			var packet = (UdpObject)o;

			Console.WriteLine("UDP in: " + packet.GetType().Name);
			if (packet.DocumentName == documentName)
			{
				if (packet.Object is FindPacket)
					ProcessUdpFind(packet.Object as FindPacket, endpoint);
				else
					ProcessCustomUdpObject(endpoint, packet);
			}
			else
				Console.WriteLine("Document mismatch");
		}

		private void ProcessTcpObject(TcpLink link, object o)
		{
			var po = o as TcpObject;
			Console.WriteLine("TCP in (" + po.Peer.EndPoint + "): " + po.Object.GetType().Name);

			ProcessCustomTcpObject(link, po);

			FirePacketArrived(po.Object, po.Peer, p =>
			{
				Console.WriteLine("TCP out (" + link + "): " + p.GetType().Name);
				link.Send(Utils.Serialize(new TcpObject() { Peer = Self, Object = p }));
			});
		}

		private void ProcessUdpFind(FindPacket p, IPEndPoint endpoint)
		{
			if (Utils.IsLocalAddress(endpoint.Address) && p.ListenPort == tcpNetwork.ListenPort)
				OwnIPDetected(new IPEndPoint(endpoint.Address, tcpNetwork.ListenPort));
			else
				tcpNetwork.EstablishConnectionTo(new IPEndPoint(endpoint.Address, p.ListenPort));
		}

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		protected bool FindPeer()
		{
			// send a broadcast with the document name into the network
			Console.WriteLine("Broadcasting for " + documentName);
			udpNetwork.BroadcastObject(new UdpObject() { DocumentName = documentName, Object = new FindPacket() { ListenPort = tcpNetwork.ListenPort } });

			// wait for an answer
			var r = tcpNetwork.WaitForTcpConnect();
			if (!r)
				Console.WriteLine("No answer. I'm first owner");
			return r;
		}

		private void OwnIPDetected(IPEndPoint address)
		{
			Self = new Peer() { EndPoint = address };
			Console.WriteLine("Own IP determined as " + address);
			ownIPWaitHandle.Set();
		}
	}
}
