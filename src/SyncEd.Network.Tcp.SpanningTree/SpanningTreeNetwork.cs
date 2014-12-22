﻿using SyncEd.Network.Packets;
using SyncEd.Network.Tcp;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections;

namespace SyncEd.Network.Tcp.SpanningTree
{
	public class SpanningTreeNetwork : BasicNetwork
	{
		private const int repairMasterNodeWaitMs = 200;
		private const int repairReestablishWaitMs = 1000;

		private bool InRepairMode { get { return repairDeadPeer != null; } }
		private List<Tuple<TcpObject, TcpLink>> repairModeOutgoingTcpPacketBuffer;
		private SortedSet<Peer> repairMasterPeers;
		private Peer repairDeadPeer;

		/// <summary>
		/// Starts the link control system which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		public override bool Start(string documentName)
		{
			repairModeOutgoingTcpPacketBuffer = new List<Tuple<TcpObject, TcpLink>>();
			repairMasterPeers = new SortedSet<Peer>(new PeerComparer());

			return base.Start(documentName);
		}

		public override void Stop()
		{
			base.Stop();
		}

		public override void SendPacket(object packet)
		{
			base.SendPacket(packet);
			TcpBroadcastObject(new TcpObject() { Peer = Self, Object = packet });
		}

		private void TcpBroadcastObject(TcpObject po, TcpLink exclude = null, bool overrideRepair = false)
		{
			if (!overrideRepair && InRepairMode)
			{
				Console.WriteLine("Buffered: " + po.Object);
				repairModeOutgoingTcpPacketBuffer.Add(Tuple.Create(po, exclude));
			}
			else
				tcpNetwork.MulticastObject(po, l => l != exclude);
		}

		protected override void PeerFailed(TcpLink link, byte[] failedData)
		{
			Console.WriteLine("PANIC - " + link + " is dead");
			RepairDeadLink(link, Self);

			// inform peers that a link died
			udpNetwork.BroadcastObject(new UdpObject() { DocumentName = documentName, Object = new PeerDiedPacket() { DeadPeer = link.Peer, RepairPeer = Self } });
		}

		protected override void ProcessCustomTcpObject(TcpLink link, TcpObject o)
		{
			// forward
			if (o.Object.GetType().IsDefined(typeof(AutoForwardAttribute), true))
				TcpBroadcastObject(o, link);
		}

		protected override void ProcessCustomUdpObject(IPEndPoint endpoint, UdpObject o)
		{
			if (o.Object is PeerDiedPacket)
				ProcessUdpPeerDied(o.Object as PeerDiedPacket);
			else
				Console.WriteLine("Warning: Unrecognized Udp Packet");
		}

		private void ProcessUdpPeerDied(PeerDiedPacket p)
		{
			if (InRepairMode)
			{
				if (!p.DeadPeer.Equals(repairDeadPeer))
					Console.WriteLine("FATAL: Incoming panic while currently repairing other node. This is not implemented =/");
				else
					lock (repairMasterPeers)
						repairMasterPeers.Add(p.RepairPeer);
			}
			else
			{
				// check all links if they are affected and kill affected ones
				TcpLink deadLink = null;
				lock (tcpNetwork.Links)
					deadLink = tcpNetwork.Links.Where(l => l.Peer.Equals(p.DeadPeer)).FirstOrDefault();
				Console.WriteLine("All links ok: " + (deadLink == null));
				if (deadLink != null)
					RepairDeadLink(deadLink, p.RepairPeer);
			}
		}

		private void RepairDeadLink(TcpLink deadLink, Peer repairMasterPeer)
		{
			lock (tcpNetwork.Links)
				tcpNetwork.Links.Remove(deadLink);
			deadLink.Dispose();

			Console.WriteLine("Preparing repair mode");
			lock (repairMasterPeers)
			{
				repairMasterPeers.Add(repairMasterPeer);

				// prevent starting repair mode multiple times
				if (!InRepairMode)
				{
					repairDeadPeer = deadLink.Peer;

					// wait a little as some more master node requests might come in
					Task.Delay(repairMasterNodeWaitMs).ContinueWith(t => Repair());
				}
			}
		}

		private void Repair()
		{
			Console.WriteLine("Repair started. Masters:");
			lock (repairMasterPeers)
				foreach (var m in repairMasterPeers)
					Console.WriteLine(m);

			// if we are not the master node, connect to it
			Peer masterNode = null;
			lock (repairMasterPeers)
				masterNode = repairMasterPeers.First();

			Console.WriteLine("Chosen?: " + (masterNode == Self));

			if (masterNode != Self)
			{
				Console.WriteLine("Connecting to repair master");
				tcpNetwork.EstablishConnectionTo(masterNode.EndPoint);
			}
			else
			{
				Console.WriteLine("Waiting for incoming connections.");
				var sw = Stopwatch.StartNew();
				while (sw.ElapsedMilliseconds < repairReestablishWaitMs)
					tcpNetwork.WaitForTcpConnect();
				sw.Stop();
			}

			// flush all packets buffered during repair
			Console.WriteLine("Flushing " + repairModeOutgoingTcpPacketBuffer.Count + " packets");
			foreach (var poAndExclude in repairModeOutgoingTcpPacketBuffer)
				TcpBroadcastObject(poAndExclude.Item1, poAndExclude.Item2, true);
			repairModeOutgoingTcpPacketBuffer.Clear();

			// notify the network
			Console.WriteLine("Send peer lost notification");
			if (masterNode == Self)
			{
				var lostPeerPacket = new LostPeerPacket() { };
				FirePacketArrived(lostPeerPacket, Self, p => { });
				TcpBroadcastObject(new TcpObject() { Peer = Self, Object = lostPeerPacket }, null, true);
			}

			// disable repair mode
			lock (repairMasterPeers)
				repairMasterPeers.Clear();
			repairDeadPeer = null;

			Console.WriteLine("Repair finished");
		}
	}
}