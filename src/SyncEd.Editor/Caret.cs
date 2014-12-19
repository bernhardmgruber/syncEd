using SyncEd.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SyncEd.Editor
{
	public class Caret : ViewModelBase
	{
		public int Position
		{
			get { return position; }
			set { SetProperty(ref position, value); }
		}
		private int position = 0;

		public Color Color
		{
			get { return color; }
			set { SetProperty(ref color, value); }
		}
		private Color color;

		public Peer Peer
		{
			get { return peer; }
			set { SetProperty(ref peer, value); }
		}
		private Peer peer;
	}
}
