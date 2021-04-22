namespace MSCMPMessages.Messages {

	class DoorsInitMessage {
		bool open;
		Vector3Message position;
	}

	class VehicleInitMessage {
		byte id;
		TransformMessage transform;
	}

	[NetMessageDesc(MessageIds.FullWorldSync)]
	class FullWorldSyncMessage {
		string mailboxName;
		int day;
		float dayTime;
		DoorsInitMessage[] doors;
		VehicleInitMessage[] vehicles;
		PickupableSpawnMessage[] pickupables;
		LightSwitchMessage[] lights;
		WeatherUpdateMessage currentWeather;

		// Informations about player we have connected to.

		Vector3Message spawnPosition;
		QuaternionMessage spawnRotation;
		byte occupiedVehicleId;
		bool passenger;
		ushort pickedUpObject;
	}
}
