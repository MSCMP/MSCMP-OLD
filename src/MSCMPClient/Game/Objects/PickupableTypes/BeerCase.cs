using System;
using UnityEngine;
using HutongGames.PlayMaker;

/// <summary>
/// Hooks events related to beercases.
/// </summary>
namespace MSCMP.Game.Objects.PickupableTypes {
	class BeerCase {
		GameObject beerCaseGO;
		Game.Components.ObjectSyncComponent osc;
		PlayMakerFSM beerCaseFSM;

		//Get used bottles
		public int UsedBottles {
			get {
				return beerCaseFSM.FsmVariables.FindFsmInt("DestroyedBottles").Value;
			}
			set {
				beerCaseFSM.FsmVariables.FindFsmInt("DestroyedBottles").Value = value;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public BeerCase(GameObject go) {
			beerCaseGO = go;
			osc = beerCaseGO.GetComponent<Components.ObjectSyncComponent>();

			Client.Assert(beerCaseFSM = Utils.GetPlaymakerScriptByName(go, "Use"), "Beer case FSM not found!");
			HookEvents();
		}

		/// <summary>
		/// Hook events.
		/// </summary>
		void HookEvents() {
			EventHook.AddWithSync(beerCaseFSM, "Remove bottle", new Func<bool>(() => {
				if (beerCaseFSM.Fsm.LastTransition.EventName == "MP_Remove bottle") {
					return true;
				}
				else {
					return false;
				}
			}));

			// Sync beer case bottle count with host.
			if (Network.NetManager.Instance.IsOnline && !Network.NetManager.Instance.IsHost) {
				osc.RequestObjectSync();
			}
		}

		/// <summary>
		/// Removes random bottles from the beer case.
		/// </summary>
		/// <param name="count">Amount of bottles that should be remaining.</param>
		public void RemoveBottles(int count) {
			int i = 0;
			while (count > UsedBottles) {
				if (UsedBottles != 23) {
					GameObject bottle = beerCaseGO.transform.GetChild(i).gameObject;
					i++;
					if (bottle != null) {
						GameObject.Destroy(bottle);
						UsedBottles++;
					}
					else {
						Logger.Error($"Failed to remove bottle! No bottle GameObjects found!");
					}
				}
				else {
					Logger.Error($"Failed to remove bottle! UsedBottles: {UsedBottles}");
				}
			}
		}
	}
}
