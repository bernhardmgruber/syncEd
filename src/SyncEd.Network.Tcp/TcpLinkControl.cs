using SyncEd.Network.Packets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace SyncEd.Network.Tcp
{
    public class TcpLinkControl : INetwork
    {
        public event DocumentPacketHandler DocumentPacketArrived;
        public event QueryDocumentPacketHandler QueryDocumentPacketArrived;
        public event AddTextPacketHandler AddTextPacketArrived;
        public event DeleteTextPacketHandler DeleteTextPacketArrived;

        private TcpLinkEstablisher establisher;
        private List<TcpPeer> peers;
        private BlockingCollection<Tuple<object, TcpPeer>> packets;

        /// <summary>
        /// Starts the link control system which is responsible for managing links and packets
        /// </summary>
        /// <returns>Returns true if a peer could be found for the given document name</returns>
        public bool Start(string documentName)
        {
            establisher = new TcpLinkEstablisher(documentName);
            establisher.NewLinkEstablished += NewLinkEstablished;
            var peer = establisher.FindPeer();
            if (peer != null)
                lock (peers)
                    peers.Add(peer);
            return peer != null;
        }

        public void Stop()
        {
            establisher.Close();
            peers.ForEach(p => p.Close());
            establisher = null;
            peers = new List<TcpPeer>();
            packets = new BlockingCollection<Tuple<object, TcpPeer>>();
        }

        void NewLinkEstablished(TcpPeer p)
        {
            lock (peers)
                peers.Add(p);
            p.ObjectReceived += ObjectReveived;
            p.Failed += PeerFailed;
        }

        void PeerFailed(TcpPeer sender)
        {
            lock (peers)
                peers.Remove(sender);
            sender.Close();
            Panic(sender);
        }

        public void SendPacket(DocumentPacket packet)
        {
            SendObject(packet);
        }

        public void SendPacket(QueryDocumentPacket packet)
        {
            SendObject(packet);
        }

        public void SendPacket(AddTextPacket packet)
        {
            SendObject(packet);
        }

        public void SendPacket(DeleteTextPacket packet)
        {
            SendObject(packet);
        }

        void SendObject(object o, Peer exclude = null)
        {
            Console.WriteLine("TcpLinkControl: Outgoing: " + o.ToString());

            lock (peers)
                foreach (TcpPeer p in peers)
                    if (p.Peer != exclude)
                        p.SendAsync(o);
        }


        void ObjectReveived(object o, Peer peer)
        {
            Console.WriteLine("TcpLinkControl: Incoming: " + o.ToString());

            // forward
            SendObject(o, peer);

            // dispatch to UI
            if (o is AddTextPacket && AddTextPacketArrived != null)
                AddTextPacketArrived(o as AddTextPacket, peer);
            else if (o is DeleteTextPacket && DeleteTextPacketArrived != null)
                DeleteTextPacketArrived(o as DeleteTextPacket, peer);
            else if (o is DocumentPacket && DocumentPacketArrived != null)
                DocumentPacketArrived(o as DocumentPacket, peer);
            else if (o is QueryDocumentPacket && QueryDocumentPacketArrived != null)
                QueryDocumentPacketArrived(o as QueryDocumentPacket, peer);
            else
                Console.WriteLine("Unrecognized packet of type: " + o.GetType().AssemblyQualifiedName);
        }

        void Panic(TcpPeer deadPeer)
        {
            Console.WriteLine("PANIC - " + deadPeer.Peer.Address + " is dead");

        }
    }
}
