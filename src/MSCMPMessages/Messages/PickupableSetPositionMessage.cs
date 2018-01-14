namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.PickupableSetPosition)]
	class PickupableSetPositionMessage {
		ushort id;
		Vector3Message position;
	}
}
