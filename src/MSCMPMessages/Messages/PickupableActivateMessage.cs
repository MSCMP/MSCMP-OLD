namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.PickupableActivate)]
	class PickupableActivateMessage {
		ushort id;
		bool activate;
	}
}
