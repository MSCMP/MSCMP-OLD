using System.Collections.Generic;

namespace MSCMPMessages.Messages {



	[NetMessageDesc(MessageIds.FullWorldSync)]
	class FullWorldSyncMessage {
		bool[] doorsOpen;
		Vector3Message[] doorsPosition;
	}
}
