using System;
using HutongGames.PlayMaker;
using UnityEngine;
using MSCMP.Game.Components;
using MSCMP.Network;

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
		/// Instance.
		/// </summary>
		public static GamePlayer Instance;


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
			Instance = this;
			
			pickupFsm = Utils.GetPlaymakerScriptByName(gameObject, "PickUp");

			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Part picked"), new OnPickupAction(this));
			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Item picked"), new OnPickupAction(this));

			PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Throw part"), new OnThrowAction(this));

			EventHook.Add(pickupFsm, "Drop part", new Func<bool>(() => {
				this.DropObject();
				return false;
			}));

			//PlayMakerUtils.AddNewAction(pickupFsm.Fsm.GetState("Drop part"), new OnDropAction(this));

			GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			trigger.transform.localScale = new Vector3(100, 100, 100);
			trigger.GetComponent<SphereCollider>().isTrigger = true;
			GameObject.Destroy(trigger.GetComponent<MeshRenderer>());

			trigger.transform.position = gameObject.transform.position;
			trigger.transform.parent = gameObject.transform;
			ObjectSyncPlayerComponent ospc = trigger.AddComponent<ObjectSyncPlayerComponent>();
		}


		/// <summary>
		/// Handle pickup of the object.
		/// </summary>
		private void PickupObject() {
			pickedUpGameObject = pickupFsm.Fsm.GetFsmGameObject("PickedObject").Value;
			ObjectSyncComponent osc = pickedUpGameObject.GetComponent<ObjectSyncComponent>();
			osc.TakeSyncControl();
			osc.SendConstantSync(true);

			Logger.Log("PickupObject " + pickedUpGameObject);
		}

		/// <summary>
		/// Handle throw of the object.
		/// </summary>
		private void ThrowObject() {
			Logger.Log("Throwed object " + pickedUpGameObject);
			pickedUpGameObject.GetComponent<ObjectSyncComponent>().SendConstantSync(false);
			pickedUpGameObject = null;
		}

		/// <summary>
		/// Handle drop of the object.
		/// </summary>
		private void DropObject() {
			Logger.Log("Drop object " + pickedUpGameObject);
			pickedUpGameObject.GetComponent<ObjectSyncComponent>().SendConstantSync(false);
			pickedUpGameObject = null;
		}

		/// <summary>
		/// Drops object when it has been stolen from the player.
		/// </summary>
		public void DropStolenObject() {
			pickupFsm.SendEvent("MP_Drop part");
		}
	}
}
