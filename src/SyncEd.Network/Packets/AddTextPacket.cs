using System;
using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
	[Serializable]
	[DataContract]
	[AutoForward]
	public class AddTextPacket
	{
		[DataMember]
		public int Offset { get; set; }

		[DataMember]
		public string Text { get; set; }
	}
}
