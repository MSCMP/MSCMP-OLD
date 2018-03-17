namespace MSCMPMessages.Messages {

	class PickedUpSync {
		Vector3Message		position;
		QuaternionMessage	rotation;
	}

	[NetMessageDesc(MessageIds.PlayerSync)]
	class PlayerSyncMessage {
		Vector3Message		position;
		QuaternionMessage	rotation;

		[Optional]
		PickedUpSync		 pickedUpData;
	}
}
