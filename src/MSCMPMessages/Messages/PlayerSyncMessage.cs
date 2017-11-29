
namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.PlayerSync)]
	class PlayerSyncMessage {
		Vector3Message position;
		QuaternionMessage rotation;
	}
}
