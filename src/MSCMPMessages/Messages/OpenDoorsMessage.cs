
namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.OpenDoors)]
	class OpenDoorsMessage {
		string doorName;
		bool open;
	}
}
