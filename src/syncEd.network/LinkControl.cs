using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network
{
	class LinkControl
	{
		public LinkEstablisher Establisher { get; set; }

		private List<Peer> peers = new List<Peer>();
		public IList<Peer> Peers { get { return peers; } }

		public LinkControl(LinkEstablisher establisher)
		{
			Establisher = establisher;
			Establisher.NewLinkEstablished += NewLinkEstablished;
		}

		void NewLinkEstablished(Peer p)
		{
			peers.Add(p);
		}


	}
}
