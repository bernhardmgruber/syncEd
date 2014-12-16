using System;
using System.Net;
using System.Runtime.Serialization;

namespace SyncEd.Network
{
	[Serializable]
	[DataContract]
	public class Peer
	{
		[DataMember]
		public IPAddress Address { get; set; }

		public override string ToString()
		{
			return "Peer {" + Address.ToString() + "}";
		}
	}
}
