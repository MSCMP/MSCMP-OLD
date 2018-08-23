namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.ObjectSync)]
	class ObjectSyncMessage {

		int objectID;

		Vector3Message position;
		QuaternionMessage rotation;

		[Optional]
		int syncType;

		[Optional]
		float[] syncedVariables;
	}
}
