namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.ObjectSyncResponse)]
	class ObjectSyncResponseMessage {

		int objectID;
		bool accepted;
	}
}
