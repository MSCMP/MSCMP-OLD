using UnityEngine;

namespace MSCMP.Game.Components {
	/// <summary>
	/// Pickupable meta data component used to associate prefab/instances with prefab id.
	/// </summary>
	class PickupableMetaDataComponent : MonoBehaviour {
		public int prefabId = -1;

		/// <summary>
		/// Register this pickupable.
		/// </summary>
		private void OnEnable() {
			GamePickupableDatabase.Instance.RegisterPickupable(gameObject);
		}

		/// <summary>
		/// Register this pickupable.
		/// </summary>
		private void OnDisable() {
			GamePickupableDatabase.Instance.UnregisterPickupable(gameObject);
		}

		/// <summary>
		/// Getter for the prefab descriptor.
		/// </summary>
		public GamePickupableDatabase.PrefabDesc PrefabDescriptor
		{
			get {
				Client.Assert(prefabId != -1, "Prefab id is not set!");
				return GamePickupableDatabase.Instance.GetPickupablePrefab(prefabId);
			}
		}
	}
}
