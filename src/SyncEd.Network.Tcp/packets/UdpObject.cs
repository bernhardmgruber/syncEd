using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	[Serializable]
	public class UdpObject
	{
		public string DocumentName { get; set; }
		public object Object { get; set; }

		public override string ToString()
		{
			return "{" + DocumentName + ", " + Object.GetType().Name + "}";
		}
	}
}
