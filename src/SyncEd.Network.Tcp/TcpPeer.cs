using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
    public delegate void ObjectReceivedHandler(object o, Peer p);

    public class TcpPeer
    {
        public event ObjectReceivedHandler ObjectReceived;

        public Peer Peer { get; private set; }

        private TcpClient Tcp { get; set; }


        private Thread sendThread;
        private Thread recvThread;

        private BlockingCollection<object> sendColl = new BlockingCollection<object>();

        private CancellationTokenSource cancelSource;

        public TcpPeer(TcpClient tcp)
        {
            Tcp = tcp;
            Peer = new Peer() { Address = (tcp.Client.RemoteEndPoint as IPEndPoint).Address };

            cancelSource = new CancellationTokenSource();
            var token = cancelSource.Token;

            sendThread = new Thread(new ThreadStart(() => {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var o = sendColl.Take(token);
                        var f = new BinaryFormatter();
                        f.Serialize(Tcp.GetStream(), o);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Send in " + ToString() + " failed: " + e);
                    }
                }
            }));
            sendThread.Start();

            recvThread = new Thread(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var f = new BinaryFormatter();
                        var o = f.Deserialize(Tcp.GetStream());
                        if (ObjectReceived != null)
                            ObjectReceived(o, Peer);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Receive in " + ToString() + " failed: " + e);
                    }
                }
            });
            recvThread.Start();
        }

        public void SendAsync(object o)
        {
            sendColl.Add(o);
        }

        public void Close()
        {
            cancelSource.Cancel();
            Tcp.GetStream().Close();
            Tcp.Close();
            sendThread.Join();
            recvThread.Join();
        }

        public override string ToString()
        {
            return "TcpPeer { " + (Tcp.Client.RemoteEndPoint as IPEndPoint).Address + "}";
        }
    }
}
