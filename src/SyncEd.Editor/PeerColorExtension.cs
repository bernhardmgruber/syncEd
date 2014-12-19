using SyncEd.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SyncEd.Editor
{
	static class PeerColorExtension
	{
		public static Color Color(this Peer peer)
		{
			int address = peer.EndPoint.Address.GetHashCode();
			short port = (short)peer.EndPoint.Port;

			short a = (short)((short)address +(short)(address >> 16));
			byte a1 = (byte)(a >> 8);
			byte a2 = (byte)(a >> 0);
			byte p = (byte)((byte)port + (byte)(port >> 8));

			// from https://social.msdn.microsoft.com/Forums/vstudio/en-US/9f52904e-dc12-4235-ad86-b691f6b91229/reverse-bits-in-byte-question?forum=csharpgeneral
			Func<byte, byte> reverse = b => {
				int rev = (b >> 4) | ((b & 0xf) << 4); 
				rev = ((rev & 0xcc) >> 2) | ((rev & 0x33) << 2); 
				rev = ((rev & 0xaa) >> 1) | ((rev & 0x55) << 1); 
				return (byte)rev; 
			};

			a1 = reverse(a1);
			a2 = reverse(a2);
			p = reverse(p);

			return new Color() { R = a1, G = a2, B = p, A = 0xFF };
		}
	}
}
