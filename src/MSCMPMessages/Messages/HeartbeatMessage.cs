namespace MSCMPMessages.Messages {

	[NetMessageDesc(MessageIds.Heartbeat)]
	class HeartbeatMessage {
		/// <summary>
		/// Local clock value used to calculate ping.
		/// </summary>
		ulong clientClock;
	}
}
