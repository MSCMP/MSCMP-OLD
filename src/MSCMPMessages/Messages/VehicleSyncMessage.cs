namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleSync)]
	class VehicleSyncMessage {

		Vector3Message position;
		QuaternionMessage rotation;

		float steering;

	}
}
