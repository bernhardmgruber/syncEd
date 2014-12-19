using System;
using System.Net;
using System.Runtime.Serialization;

namespace SyncEd.Network
{
	[Serializable]
	[DataContract]
	public class Peer
	{
		/// <summary>
		/// Uniquly identifies the peer on the network by specifying his address and the port of his TCP listener (unique on the same machine)
		/// </summary>
		[DataMember]
		public IPEndPoint EndPoint { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is Peer)
				return (obj as Peer).EndPoint.Equals(EndPoint);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return EndPoint.GetHashCode();
		}

		public override string ToString()
		{
			return "Peer {" + EndPoint.ToString() + "}";
		}
	}
}
