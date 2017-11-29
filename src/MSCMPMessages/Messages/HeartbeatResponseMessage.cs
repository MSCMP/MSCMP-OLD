using System;


namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.HeartbeatResponse)]
	class HeartbeatResponseMessage {

		/// <summary>
		/// The clock received in Heartbeat packet. Used to calculate ping.
		/// </summary>
		UInt64 clientClock;

		/// <summary>
		/// Our clock value.
		/// </summary>
		UInt64 clock;
	}
}
