namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.HeartbeatResponse)]
	class HeartbeatResponseMessage {

		/// <summary>
		/// The clock received in Heartbeat packet. Used to calculate ping.
		/// </summary>
		ulong clientClock;

		/// <summary>
		/// Our clock value.
		/// </summary>
		ulong clock;
	}
}
