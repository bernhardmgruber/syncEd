using System;
using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
	[Serializable]
	[DataContract]
	[AutoForward]
	public class DeleteTextPacket
	{
		[DataMember]
		public int Offset { get; set; }

		[DataMember]
		public int Length { get; set; }
	}
}
