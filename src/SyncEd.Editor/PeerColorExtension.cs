using SyncEd.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SyncEd.Editor
{
	static class PeerColorExtension
	{
		public static Color Color(this Peer peer)
		{
			int hash = peer.Address.GetHashCode();
			return new Color() { R = (byte)(hash >> 16), G = (byte)(hash >> 8), B = (byte)(hash >> 0), A = 0xFF };
		}
	}
}
