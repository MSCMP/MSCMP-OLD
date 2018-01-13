using UnityEngine;
using MSCMP.Network;

namespace MSCMP.Game.Components {
	/// <summary>
	/// Component used to track life of the pickupable.
	/// </summary>
	class PickupableLifeTrackerComponent : MonoBehaviour {

		/// <summary>
		/// The network world owning the pickupable.
		/// </summary>
		public NetWorld netWorld;

		void OnDestroy() {
			if (netWorld != null) {
				netWorld.HandlePickupableDestroy(gameObject);
			}
		}
	}
}
