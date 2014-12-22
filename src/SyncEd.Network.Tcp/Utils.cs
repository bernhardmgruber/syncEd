using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public static class Utils
	{
		private static BinaryFormatter formatter = new BinaryFormatter();

		public static byte[] Serialize(object o)
		{
			using (var ms = new MemoryStream())
			{
				// serialize packet
				formatter.Serialize(ms, o);

				// shrink buffer
				byte[] bytes = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(bytes, 0, (int)ms.Length);

				return bytes;
			}
		}

		public static object Deserialize(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
				return formatter.Deserialize(ms);
		}

		public static bool IsLocalAddress(IPAddress address)
		{
			return Dns.GetHostAddresses(Dns.GetHostName()).Any(a => a.Equals(address));
		}
	}
}
