using System;
using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

/// <summary>
/// Syncs items on shelves at the store as well as the cash registers, and switches behind counter.
/// </summary>
namespace MSCMP.Game.Places {
	class Shop {
		private GameObject shopGO;
		private GameObject shopProducts;
		private GameObject pubProducts;
		private GameObject shopRegister;
		private GameObject pubRegister;

		private PlayMakerFSM switchPumps;
		private PlayMakerFSM switchDoor;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Shop(GameObject shop) {
			shopGO = shop;
			foreach (Transform transform in shopGO.GetComponentsInChildren<Transform>()) {
				switch (transform.name) {
					// Products on shelves.
					case "ActivateStore":
						shopProducts = transform.gameObject;
						break;

					// Products in pub.
					case "ActivateBar":
						pubProducts = transform.gameObject;
						break;

					// Cash register in store.
					case "Register":
						if (transform.parent.name == "StoreCashRegister") {
							shopRegister = transform.gameObject;
						}
						if (transform.parent.name == "PubCashRegister") {
							pubRegister = transform.gameObject;
						}
						break;
					// Switches behind counter.
					case "switch_pumps":
						switchPumps = Utils.GetPlaymakerScriptByName(transform.gameObject, "Use");
						break;
					case "switch_door":
						switchDoor = Utils.GetPlaymakerScriptByName(transform.gameObject, "Use");
						break;
				}
			}

			HookEvents();
		}

		/// <summary>
		/// Hook events.
		/// </summary>
		void HookEvents() {
			// Products on shelves.
			foreach (PlayMakerFSM fsm in shopProducts.GetComponentsInChildren<PlayMakerFSM>()) {
				if (fsm.FsmName == "Buy") {
					EventHook.AddWithSync(fsm, "Remove");
					EventHook.AddWithSync(fsm, "Reset");
				}
				// Yes, the fan belt has a different state. :thonking:
				if (fsm.gameObject.name == "BuyFanbelt") {
					EventHook.AddWithSync(fsm, "Play anim 2");
				}
				else {
					EventHook.AddWithSync(fsm, "Play anim");
				}
			}
			// Products in pub.
			foreach (PlayMakerFSM fsm in pubProducts.GetComponentsInChildren<PlayMakerFSM>()) {
				if (fsm.FsmName == "Buy") {
					EventHook.AddWithSync(fsm, "Check money", new Func<bool>(() => {
						if (fsm.Fsm.PreviousActiveState.Name.StartsWith("MP_") && Network.NetManager.Instance.IsHost) {
							Logger.Log("Ignoring 'Check money' event!");
							return true;
						}
						else {
							Logger.Log("Previous state is: " + fsm.Fsm.PreviousActiveState.Name);
							return false;
						}
					}));
				}
			}

			// Cash register in store.
			PlayMakerFSM storeRegisterFSM = Utils.GetPlaymakerScriptByName(shopRegister, "Data");
			EventHook.AddWithSync(storeRegisterFSM, "Check money");

			// Switches behind counter.
			EventHook.AddWithSync(switchPumps, "ON");
			EventHook.AddWithSync(switchPumps, "OFF");

			EventHook.AddWithSync(switchDoor, "ON");
			EventHook.AddWithSync(switchDoor, "OFF");
		}
	}
}
