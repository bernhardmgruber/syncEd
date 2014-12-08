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
        public event DocumentPacketHandler      DocumentPacketArrived;
        public event QueryDocumentPacketHandler QueryDocumentPacketArrived;
        public event AddTextPacketHandler       AddTextPacketArrived;
        public event DeleteTextPacketHandler    DeleteTextPacketArrived;

        public TcpLinkEstablisher Establisher { get; set; }

        private List<TcpPeer> peers = new List<TcpPeer>();
        public IList<TcpPeer> Peers { get { return peers; } }

        private BlockingCollection<Tuple<object, TcpPeer>> packets = new BlockingCollection<Tuple<object, TcpPeer>>();

        private CancellationTokenSource cancelSrc;

        public TcpLinkControl(TcpLinkEstablisher establisher)
        {
            Establisher = establisher;
            Establisher.NewLinkEstablished += NewLinkEstablished;
        }

        void NewLinkEstablished(TcpPeer p)
        {
            peers.Add(p);

            var packetTask = Task.Run(() => {
                while (true) {
                    lock (p) {
                        if (!p.Tcp.Connected)
                            break;

                        var f = new BinaryFormatter();
                        var packet = f.Deserialize(p.Tcp.GetStream());
                        packets.Add(Tuple.Create(packet, p));
                    }

                    p.Tcp.GetStream().Close();
                    p.Tcp.Close();
                    Console.WriteLine("Lost peer " + p);

                    // TODO, reconnect network
                }
            });
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

        void SendObject(object o)
        {
            packets.Add(Tuple.Create(o, null as TcpPeer));
        }

        /// <summary>
        /// Starts the link control system which is responsible for managing links and packets
        /// </summary>
        /// <returns>Returns true if a peer could be found for the given document name</returns>
        public bool Start(string documentName)
        {
            cancelSrc = new CancellationTokenSource();
            var token = cancelSrc.Token;

            var peer = Establisher.FindPeer(documentName);
            if (peer != null)
                peers.Add(peer);
            Establisher.ListenForPeers(documentName, token);

            Task.Run(() => {
                while (!token.IsCancellationRequested) {
                    var packetAndPeer = packets.Take(token);

                    Console.WriteLine("TcpLinkControl: Outgoing: " + packetAndPeer.Item1.ToString());

                    foreach (TcpPeer p in peers) {
                        if (p != null && p != packetAndPeer.Item2)
                            lock (p) {
                                var f = new BinaryFormatter();
                                f.Serialize(p.Tcp.GetStream(), packetAndPeer.Item1);
                            }
                    }

                    DispatchObject(packetAndPeer.Item1, packetAndPeer.Item2.Peer);
                    
                }
            });

            return peer != null;
        }

        void DispatchObject(object o, Peer peer)
        {
            Console.WriteLine("TcpLinkControl: Incoming: " + o.ToString());

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

        public void Stop()
        {
            cancelSrc.Cancel();

            peers.ForEach(p => {
                lock (p) {
                    p.Tcp.Client.Disconnect(false);
                }
            });
        }
    }
}
