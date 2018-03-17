namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleEnter)]
	class VehicleEnterMessage {
		/// <summary>
		/// Network vehicle of the vehicle player is entering to.
		/// </summary>
		byte vehicleId;

		/// <summary>
		/// Is player entering passenger seat?
		/// </summary>
		bool passenger;
	}
}
