using System;
using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
	[Serializable]
	[DataContract]
	public class PeerCountPacket
	{
		[DataMember]
		public int Count { get; set; }
	}
}
