namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleSync)]
	class VehicleSyncMessage {

		Vector3Message position;
		QuaternionMessage rotation;

		float steering;
		float throttle;
		float brake;
		float clutch;
		float fuel;

		[Optional]
		int gear;

		[Optional]
		bool range;

		[Optional]
		float hydraulic;
	}
}
