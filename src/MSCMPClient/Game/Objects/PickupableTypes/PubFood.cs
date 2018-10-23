using System;
using UnityEngine;
using HutongGames.PlayMaker;

/// <summary>
/// Hooks events for food purchased in the pub.
/// </summary>
namespace MSCMP.Game.Objects.PickupableTypes {
	class PubFood {
		GameObject FoodGO;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="go"></param>
		public PubFood(GameObject go) {
			FoodGO = go;

			HookEvents();
		}

		/// <summary>
		/// Hook events for pub food.
		/// </summary>
		void HookEvents() {
			PlayMakerFSM foodFSM = Utils.GetPlaymakerScriptByName(FoodGO, "Use");
			EventHook.AddWithSync(foodFSM, "State 2");
		}
	}
}
