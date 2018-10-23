namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.PickupableActivate)]
	class PickupableActivateMessage {
		int id;
		bool activate;
	}
}
