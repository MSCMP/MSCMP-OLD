namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.EventHookSync)]
	class EventHookSyncMessage {
		int fsmID;
		int fsmEventID;
		bool request;

		[Optional]
		string fsmEventName;
	}
}
