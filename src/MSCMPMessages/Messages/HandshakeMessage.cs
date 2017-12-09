using System;

namespace MSCMPMessages.Messages {

	[NetMessageDesc(MessageIds.Handshake)]
	class HandshakeMessage {

		UInt64				clock;

		// Informations about player we have connected to.

		Vector3Message		spawnPosition;
		QuaternionMessage	spawnRotation;
		byte				occupiedVehicleId;
		bool				passenger;
		ushort				pickedUpObject;
	}
}
