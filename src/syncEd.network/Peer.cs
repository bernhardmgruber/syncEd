﻿using System;
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

		public override bool Equals(object obj)
		{
			if (obj is Peer)
				return (obj as Peer).Address.Equals(Address);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return Address.GetHashCode();
		}

		public override string ToString()
		{
			return "Peer {" + Address.ToString() + "}";
		}
	}
}
