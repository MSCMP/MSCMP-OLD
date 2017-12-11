namespace MSCMPMessages.Messages {

	class DoorsInitMessage {
		bool open;
		Vector3Message position;
	}

	class VehicleInitMessage {
		byte id;
		TransformMessage transform;
	}

	class PickupableInitMessage {
		ushort id;
		TransformMessage transform;
	}

	[NetMessageDesc(MessageIds.FullWorldSync)]
	class FullWorldSyncMessage {
		DoorsInitMessage[]			doors;
		VehicleInitMessage[]		vehicles;
		PickupableInitMessage[]		pickupables;
	}
}
