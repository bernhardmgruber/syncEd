using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	public class TcpObject
	{
		public Peer Peer { get; set; }
		public object Object { get; set; }

		public override string ToString()
		{
			return "TcpObject {" + Peer + ", " + Object + "}";
		}
	}
}
