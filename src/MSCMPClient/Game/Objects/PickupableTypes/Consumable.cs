using MSCMP.Game;
using UnityEngine;
using HutongGames.PlayMaker;

/// <summary>
/// Syncs consuming of consumable items. (Such as food and drink)
/// </summary>
namespace MSCMP.Game.Objects.PickupableTypes {
	class Consumable {
		GameObject itemGO;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Consumable(GameObject go) {
			itemGO = go;
			HookEvents();
		}

		/// <summary>
		/// Hook events for food or drink items.
		/// </summary>
		public void HookEvents() {
			foreach (PlayMakerFSM fsm in itemGO.GetComponents<PlayMakerFSM>()) {
				if (fsm.Fsm.Name == "Use") {
					EventHook.AddWithSync(fsm, "Destroy");
				}
			}
		}
	}
}
