namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleSwitch)]
	class VehicleSwitchMessage {
		/// <summary>
		/// Object ID of the vehicle player is entering.
		/// </summary>
		int objectID;

		/// <summary>
		/// ID of switch to change.
		/// </summary>
		int switchID;

		/// <summary>
		/// Value of the switch. (Bool)
		/// </summary>
		bool switchValue;

		/// <summary>
		/// Value of the switch. (Float)
		/// </summary>
		[Optional]
		float switchValueFloat;
	}
}
