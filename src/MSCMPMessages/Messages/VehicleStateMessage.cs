namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleState)]
	class VehicleStateMessage {
		int objectID;
		int state;
		int dashstate;

		[Optional]
		float startTime;
	}
}
