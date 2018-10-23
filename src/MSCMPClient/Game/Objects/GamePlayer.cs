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
		/// Constructor.
		/// </summary>
		/// <param name="gameObject">The game object to pickup.</param>
		public GamePlayer(GameObject gameObject) {
			this.gameObject = gameObject;
			Instance = this;
			
			pickupFsm = Utils.GetPlaymakerScriptByName(gameObject, "PickUp");

			if (pickupFsm != null) {
				// Pickup events
				EventHook.Add(pickupFsm, "Part picked", new Func<bool>(() => {
					this.PickupObject();
					return false;
				}));
				EventHook.Add(pickupFsm, "Item picked", new Func<bool>(() => {
					this.PickupObject();
					return false;
				}));

				// Throw event
				EventHook.Add(pickupFsm, "Throw part", new Func<bool>(() => {
					this.ThrowObject();
					return false;
				}));

				// Drop event
				EventHook.Add(pickupFsm, "Drop part", new Func<bool>(() => {
					this.DropObject();
					return false;
				}));
			}

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

			Logger.Log("Picked up object: " + pickedUpGameObject);
		}

		/// <summary>
		/// Handle throw of the object.
		/// </summary>
		private void ThrowObject() {
			Logger.Log("Threw object: " + pickedUpGameObject);
			pickedUpGameObject.GetComponent<ObjectSyncComponent>().SendConstantSync(false);
			pickedUpGameObject = null;
		}

		/// <summary>
		/// Handle drop of the object.
		/// </summary>
		private void DropObject() {
			Logger.Log("Dropped object: " + pickedUpGameObject);
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
