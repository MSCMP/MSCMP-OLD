namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.AnimSync)]
	class AnimSyncMessage {
		bool isRunning;
		bool isLeaning;
		bool isGrounded;
		byte activeHandState;
		float aimRot;
		float crouchPosition;
		bool isDrunk;
		byte drinkId;
		int swearId;
	}
}
