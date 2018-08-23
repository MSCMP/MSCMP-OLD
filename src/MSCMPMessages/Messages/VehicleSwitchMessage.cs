namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleSwitch)]
	class VehicleSwitchMessage {
		byte vehicleId;

		int switchID;
		bool switchValue;

		[Optional]
		float switchValueFloat;
	}
}
