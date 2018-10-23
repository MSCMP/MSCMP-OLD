using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace MSCMP.Game.Objects.PickupableTypes {
	class ShoppingBag {
		GameObject ShoppingBagGO;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="go"></param>
		public ShoppingBag(GameObject go) {
			ShoppingBagGO = go;
			HookEvents();
		}

		/// <summary>
		/// Hook events for shopping bag.
		/// </summary>
		void HookEvents() {
			// Shopping bag.
			PlayMakerFSM bagFSM = Utils.GetPlaymakerScriptByName(ShoppingBagGO, "Open");
			EventHook.AddWithSync(bagFSM, "Play anim");
			// This class also handles the fireworks bag.
		}
	}
}
