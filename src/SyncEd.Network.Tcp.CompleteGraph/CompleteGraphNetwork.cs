using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp.CompleteGraph
{
	public class CompleteGraphNetwork : BasicNetwork
	{
		public override bool Start(string documentName)
		{
			base.Start(documentName);


			return true;
		}

		public override void Stop()
		{
			base.Stop();
		}

		public override void SendPacket(object packet)
		{
			base.SendPacket(packet);
		}


		protected override void PeerFailed(TcpLink link, byte[] failedData)
		{
			Console.WriteLine("Lost connection to: " + link);
		}


		protected override void ProcessCustomTcpObject(TcpLink link, TcpObject o)
		{
			throw new NotImplementedException();
		}

		protected override void ProcessCustomUdpObject(System.Net.IPEndPoint endpoint, UdpObject o)
		{
			throw new NotImplementedException();
		}
	}
}
