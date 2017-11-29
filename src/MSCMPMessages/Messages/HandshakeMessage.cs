using System;

namespace MSCMPMessages.Messages {

	[NetMessageDesc(MessageIds.Handshake)]
	class HandshakeMessage {

		UInt64 clock;
	}
}
