namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleEnter)]
	class VehicleEnterMessage {
		/// <summary>
		/// Object ID of the vehicle player is entering.
		/// </summary>
		int objectID;

		/// <summary>
		/// Is player entering passenger seat?
		/// </summary>
		bool passenger;
	}
}
