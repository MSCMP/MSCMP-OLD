using HutongGames.PlayMaker;
using UnityEngine;

namespace MSCMP.Game.Objects {
	/// <summary>
	/// Class representing local player object.
	/// </summary>
	class GamePlayer {
		PlayMakerFSM pickupFsm = null;

		private GameObject gameObject = null;
		private GameObject pickedUpGameObject = null;

		/// <summary>
		/// Get game object representing player.
		/// </summary>
		public GameObject Object {
			get { return gameObject; }
		}

		/// <summary>
		/// Get object player has picked up.
		/// </summary>
		public GameObject PickedUpObject {
			get { return pickedUpGameObject;  }
		}


		/// <summary>
		/// Action executed when player pickups some game object.
		/// </summary>
		private class OnPickupAction : FsmStateAction {

			GamePlayer player = null;

			public OnPickupAction(GamePlayer player) {
				this.player = player;
			}

			public override void OnEnter() {
				player.PickupObject();
				Finish();
			}
		}

		/// <summary>
		/// Action executed when player throws some game object.
		/// </summary>
		private class OnThrowAction : FsmStateAction {

			GamePlayer player = null;

			public OnThrowAction(GamePlayer player) {
				this.player = player;
			}

			public override void OnEnter() {
				player.ThrowObject();
				Finish();
			}
		}

		/// <summary>
		/// Action executed when player drops some game object.
		/// </summary>
		private class OnDropAction : FsmStateAction {

			GamePlayer player = null;

			public OnDropAction(GamePlayer player) {
				this.player = player;
			}

			public override void OnEnter() {
				player.DropObject();
				Finish();
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameObject">The game object to pickup.</param>
		public GamePlayer(GameObject gameObject) {
			this.gameObject = gameObject;

			pickupFsm = Utils.GetPlaymakerScriptByName(gameObject, "PickUp");

			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Part picked"), new OnPickupAction(this));
			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Item picked"), new OnPickupAction(this));

			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Throw part"), new OnThrowAction(this));
			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Drop part"), new OnDropAction(this));
		}


		/// <summary>
		/// Handle pickup of the object.
		/// </summary>
		private void PickupObject() {
			pickedUpGameObject = pickupFsm.Fsm.GetFsmGameObject("PickedObject").Value;
			Logger.Log("PickupObject " + pickedUpGameObject);

			if (GameCallbacks.onObjectPickup != null) {
				GameCallbacks.onObjectPickup(pickedUpGameObject);
			}
		}

		/// <summary>
		/// Handle throw of the object.
		/// </summary>
		private void ThrowObject() {
			Logger.Log("Throwed object " + pickedUpGameObject);
			pickedUpGameObject = null;

			if (GameCallbacks.onObjectRelease != null) {
				GameCallbacks.onObjectRelease(false);
			}
		}

		/// <summary>
		/// Handle drop of the object.
		/// </summary>
		private void DropObject() {
			Logger.Log($"Drop object {pickedUpGameObject}");
			pickedUpGameObject = null;

			if (GameCallbacks.onObjectRelease != null) {
				GameCallbacks.onObjectRelease(true);
			}
		}
	}
}
