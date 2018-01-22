using UnityEngine;

namespace MSCMP.Game.Components {
	/// <summary>
	/// Pickupable meta data component used to associate prefab/instances with prefab id.
	/// </summary>
	class PickupableMetaDataComponent : MonoBehaviour {
		public int prefabId = -1;

		/// <summary>
		/// Getter for the prefab descriptor.
		/// </summary>
		public GamePickupableDatabase.PrefabDesc PrefabDescriptor
		{
			get {
				if (prefabId != -1) {
					return GamePickupableDatabase.Instance.GetPickupablePrefab(prefabId);
				}
				return null;
			}
		}
	}
}
