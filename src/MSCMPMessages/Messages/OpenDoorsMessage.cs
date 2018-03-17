namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.OpenDoors)]
	class OpenDoorsMessage {
		Vector3Message position;
		bool open;
	}
}
