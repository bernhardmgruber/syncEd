using SyncEd.Network;
using System.Windows.Media;

namespace SyncEd.Editor
{
    static class PeerColorExtension
    {
        public static Color Color(this Peer peer)
        {
            double h = (double)(peer.EndPoint.Address.GetHashCode() * peer.EndPoint.Port.GetHashCode()) / int.MaxValue;
            return HslToRgb(h, 1.0, 0.5);

            /*int address = peer.Address.Address.GetHashCode();
            short port = (short)peer.Address.Port;

            short a = (short)((short)address + (short)(address >> 16));
            byte a1 = (byte)(a >> 8);
            byte a2 = (byte)(a >> 0);
            byte p = (byte)((byte)port + (byte)(port >> 8));

            // from https://social.msdn.microsoft.com/Forums/vstudio/en-US/9f52904e-dc12-4235-ad86-b691f6b91229/reverse-bits-in-byte-question?forum=csharpgeneral
            Func<byte, byte> reverse = b => {
                int rev = (b >> 4) | ((b & 0xf) << 4);
                rev = ((rev & 0xcc) >> 2) | ((rev & 0x33) << 2);
                rev = ((rev & 0xaa) >> 1) | ((rev & 0x55) << 1);

                rev = rev >> 1 + 128;
                return (byte)rev;
            };

            a1 = reverse(a1);
            a2 = reverse(a2);
            p = reverse(p);

            return new Color() { R = a1, G = a2, B = p, A = 0xFF };*/
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double r = 0, g = 0, b = 0;

            if (l == 0) {
                r = g = b = 0;
            } else {
                if (s == 0) {
                    r = g = b = l;
                } else {
                    double temp2 = ((l <= 0.5) ? l * (1.0 + s) : l + s - (l * s));
                    double temp1 = 2.0 * l - temp2;

                    double[] t3 = { h + 1.0 / 3.0, h, h - 1.0 / 3.0 };
                    double[] clr = { 0, 0, 0 };
                    for (int i = 0; i < 3; i++) {
                        if (t3[i] < 0)
                            t3[i] += 1.0;
                        if (t3[i] > 1)
                            t3[i] -= 1.0;

                        if (6.0 * t3[i] < 1.0)
                            clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0;
                        else if (2.0 * t3[i] < 1.0)
                            clr[i] = temp2;
                        else if (3.0 * t3[i] < 2.0)
                            clr[i] = (temp1 + (temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0);
                        else
                            clr[i] = temp1;
                    }
                    r = clr[0];
                    g = clr[1];
                    b = clr[2];
                }
            }
            return new Color { R = (byte)(255 * r), G = (byte)(255 * g), B = (byte)(255 * b), A = 0xFF };
        }
    }
}
