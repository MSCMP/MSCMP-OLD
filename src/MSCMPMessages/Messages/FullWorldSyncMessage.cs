namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.FullWorldSync)]
	class FullWorldSyncMessage {
		// Doors state

		bool[] doorsOpen;
		Vector3Message[] doorsPosition;

		// Vehicles state
		byte[] vehicleId;
		Vector3Message[] vehiclesPosition;
		QuaternionMessage[] vehiclesRotation;
	}
}
