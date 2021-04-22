using UnityEngine;
using System;

namespace MSCMP.Network {
	/// <summary>
	/// Network object using for synchronization of pickupable.
	/// </summary>
	class NetPickupable {

		/// <summary>
		/// Invalid id of the pickupable.
		/// </summary>
		public const ushort INVALID_ID = UInt16.MaxValue;

		/// <summary>
		/// The network id of the pickupable.
		/// </summary>
		ushort netId;

		/// <summary>
		/// Network id of this pickupable.
		/// </summary>
		public ushort NetId {
			get { return netId; }
		}

		/// <summary>
		/// The game object representing pickupable.
		/// </summary>
		public GameObject gameObject;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netId">The network id of the pickupable.</param>
		/// <param name="go">The game object representing pickupable.</param>
		public NetPickupable(ushort netId, GameObject go) {
			this.netId = netId;
			this.gameObject = go;
		}
	}
}
