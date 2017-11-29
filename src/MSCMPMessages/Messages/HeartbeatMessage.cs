using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSCMPMessages.Messages {

	[NetMessageDesc(MessageIds.Heartbeat)]
	class HeartbeatMessage {
		/// <summary>
		/// Local clock value used to calculate ping.
		/// </summary>
		UInt64 clientClock;
	}
}
