namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.PickupableSpawn)]
	class PickupableSpawnMessage {
		/// <summary>
		/// Network id of the pickupable to spawn.
		/// </summary>
		ushort				id;

		/// <summary>
		/// The prefab used to create given pickupable.
		/// </summary>
		int					prefabId;

		/// <summary>
		/// The spawn transformation of the pickupable.
		/// </summary>
		TransformMessage	transform;

		/// <summary>
		/// Should this pickupable be spawned as active?
		/// </summary>
		bool				active;

		/// <summary>
		/// Optional data to send with the pickupable.
		/// </summary>
		[Optional]
		float[]				data;
	}
}
