using System;

namespace SyncEd.Document
{
	public class PeerCountChangedEventArgs : EventArgs
	{
		public int Count { get; private set; }

		public PeerCountChangedEventArgs(int count)
		{
			Count = count;
		}
	}
}
