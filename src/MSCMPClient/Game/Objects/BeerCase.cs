using HutongGames.PlayMaker;
using System.Collections.Generic;
using UnityEngine;
using System;
using MSCMP.Game;

namespace MSCMP.Game.Objects {

	/// <summary>
	/// Beercase wrapper
	/// </summary>
	class BeerCase {
		GameObject go = null;
		PlayMakerFSM fsm = null;

		//Get used bottles
		public int UsedBottles {
			get {
				return fsm.FsmVariables.FindFsmInt("DestroyedBottles").Value;
			}
			set {
				fsm.FsmVariables.FindFsmInt("DestroyedBottles").Value = value;
			}
		}

		/// <summary>
		/// Position of the bottlecase in world.
		/// </summary>
		public Vector3 Position {
			get {
				return go.transform.position;
			}
		}

		public delegate void OnConsumedBeer(GameObject beercase);

		//Callback called when beer is consumed.
		public OnConsumedBeer onConsumedBeer;

		private const string EVENT_NAME = "MPBOTTLE";

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameObject">Game object of the beercases to represent by this wrapper.</param>
		public BeerCase(GameObject gameObject) {
			go = gameObject;

			fsm = Utils.GetPlaymakerScriptByName(go, "Use");
			if (fsm.Fsm.HasEvent(EVENT_NAME)) {
				//Already hooked
				Logger.Debug($"Beercase {go.name} is already hooked!");
			}
			else {
				FsmEvent mpEvent = fsm.Fsm.GetEvent(EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(fsm, mpEvent, "Remove bottle");
				PlayMakerUtils.AddNewAction(fsm.Fsm.GetState("Remove bottle"), new OnConsumeBeerAction(this));
			}

			Logger.Debug($"Beercase found!");
		}

		/// <summary>
		/// PlayMaker state action executed when a beer bottle is consumed
		/// </summary>
		private class OnConsumeBeerAction : FsmStateAction {
			private BeerCase beerCase;

			public OnConsumeBeerAction(BeerCase beer) {
				beerCase = beer;
			}

			public override void OnEnter() {
				Finish();
				Logger.Debug($"Beer bottle consumed! Beercase: {beerCase.go.name} Used bottles: {beerCase.UsedBottles + 1}");

				// If remove bottle was triggered from our custom event we do not send it.
				if (State.Fsm.LastTransition.EventName == EVENT_NAME) {
					return;
				}

				beerCase.onConsumedBeer(beerCase.go);
			}
		}

		/// <summary>
		/// Removes randoom bottles from the beercase
		/// </summary>
		/// <param name="count">Amount of bottles to remove</param>
		public void RemoveBottles(int count) {
			while (count-- > 0) {
				if (UsedBottles != BeerCaseManager.Instance.FullCaseBottles) {
					GameObject bottle = go.transform.GetChild(count).gameObject;
					if (bottle != null) {
						Logger.Log($"Bottle removed, UsedBottles: {UsedBottles}");
						GameObject.Destroy(bottle);
						UsedBottles++;
					}
					else {
						Logger.Log($"Failed to remove bottle! No bottle objects found!");
					}
				}
				else {
					Logger.Log($"Failed to remove bottle! UsedBottles: {UsedBottles}");
				}
			}
		}

		/// <summary>
		/// Returns GameObject of BeerCase.
		/// </summary>
		/// <returns>GameObject</returns>
		public GameObject GetGameObject {
			get {
				return go;
			}
		}
	}
}
