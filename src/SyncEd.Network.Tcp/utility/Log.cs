using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public class Log
	{
		public static void WriteLine(object o)
		{
			WriteLine(o.ToString());
		}

		public static void WriteLine(string str)
		{
			var now = DateTime.Now;
			Console.WriteLine(String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3} {4}", now.Hour, now.Minute, now.Second, now.Millisecond, str));
		}
	}
}
