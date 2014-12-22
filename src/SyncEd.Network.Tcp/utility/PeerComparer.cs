using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public class PeerComparer : IComparer<Peer>
	{
		private int CompareBytes(byte[] a, byte[] b)
		{
			for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
			{
				int r = a[i] - b[i];
				if (r != 0)
					return r;
				continue;
			}

			return a.Length - b.Length;
		}

		public int Compare(Peer a, Peer b)
		{
			int r = CompareBytes(a.EndPoint.Address.GetAddressBytes(), b.EndPoint.Address.GetAddressBytes());
			if (r != 0)
				return r;
			return a.EndPoint.Port.CompareTo(b.EndPoint.Port);
		}
	}
}
