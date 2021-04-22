using System;

namespace MSCMPMessages.Messages {

	[NetMessageDesc(MessageIds.Handshake)]
	class HandshakeMessage {
		/// <summary>
		/// Protocol version of the client that sent this message.
		/// </summary>
		int protocolVersion;

		/// <summary>
		/// Network clock of the client that sent this message.
		/// </summary>
		UInt64 clock;
	}
}
