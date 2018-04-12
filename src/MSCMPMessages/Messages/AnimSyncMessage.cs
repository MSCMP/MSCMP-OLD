namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.AnimSync)]
	class AnimSyncMessage {
		System.Boolean      isLeaning;
		System.Boolean		isGrounded;
		byte				activeHandState;
		float				aimRot;
		System.Boolean		isDrunk;
	}
}
