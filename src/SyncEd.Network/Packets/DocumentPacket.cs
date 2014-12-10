﻿using System;
using System.Runtime.Serialization;

namespace SyncEd.Network.Packets
{
    [Serializable]
    [DataContract]
    public class DocumentPacket
    {
        [DataMember]
        public string Document { get; set; }
    }
}
