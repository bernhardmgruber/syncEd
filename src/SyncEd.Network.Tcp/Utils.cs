using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	internal class Utils
	{
		private static BinaryFormatter f = new BinaryFormatter();

		internal static byte[] Serialize(object o)
		{
			using (var ms = new MemoryStream())
			{
				// serialize packet
				f.Serialize(ms, o);

				// shrink buffer
				byte[] bytes = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(bytes, 0, (int)ms.Length);

				return bytes;
			}
		}

		internal static object Deserialize(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
				return f.Deserialize(ms);
		}

		internal byte[] ToBytes(string str)
		{
			return Encoding.Unicode.GetBytes(str);
		}

		internal string ToString(byte[] bytes)
		{
			return Encoding.Unicode.GetString(bytes);
		}
	}
}
