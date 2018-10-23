using System;
using MSCMP.Game.Objects.PickupableTypes;
using UnityEngine;

namespace MSCMP.Game.Objects {
	class Pickupable : ISyncedObject {

		GameObject gameObject;
		Rigidbody rigidbody;

		bool holdingObject = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Pickupable(GameObject go) {
			gameObject = go;
			rigidbody = go.GetComponent<Rigidbody>();

			// Determine pickupable subtype by GameObject name.
			if (gameObject.name == "Sausage-Potatoes(Clone)") {
				new PubFood(gameObject);
				return;
			}

			// Determines pickupable subtype by FSM contents.
			PlayMakerFSM[] fsms = go.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				// Consumable.
				if (fsm.Fsm.GetState("Eat") != null || fsm.Fsm.GetState("Eat 2") != null) {
					new Consumable(gameObject);
					break;
				}
				else if (fsm.Fsm.GetState("Initiate") != null && fsm.Fsm.Name == "Open") {
					new ShoppingBag(gameObject);
					break;
				}
				// Insert other stuff here, such as beercases.
			}
		}

		/// <summary>
		/// Get object's Transform.
		/// </summary>
		/// <returns>Object's Transform.</returns>
		public Transform ObjectTransform() {
			return gameObject.transform;
		}

		/// <summary>
		/// Check is periodic sync of the object is enabled.
		/// </summary>
		/// <returns>Periodic sync enabled or disabled.</returns>
		public bool PeriodicSyncEnabled() {
			return false;
		}

		/// <summary>
		/// Determines if the object should be synced.
		/// </summary>
		/// <returns>True if object should be synced, false if it shouldn't.</returns>
		public bool CanSync() {
			if (rigidbody.velocity.sqrMagnitude >= 0.01f) {
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// Called when a player enters range of an object.
		/// </summary>
		/// <returns>True if the player should try to take ownership of the object.</returns>
		public bool ShouldTakeOwnership() {
			return true;
		}

		/// <summary>
		/// Returns variables to be sent to the remote client.
		/// </summary>
		/// <returns>Variables to be sent to the remote client.</returns>
		public float[] ReturnSyncedVariables() {
			if (holdingObject) {
				float[] variables = { 1 };
				return variables;
			}
			else {
				float[] variables = { 0 };
				return variables;
			}
		}

		/// <summary>
		/// Handle variables sent from the remote client.
		/// </summary>
		public void HandleSyncedVariables(float[] variables) {
			if (rigidbody != null) {
				if (variables[0] == 1) {
					// Object is being held.
					rigidbody.useGravity = false;
				}
				else {
					// Object is not being held.
					rigidbody.useGravity = true;
				}
			}
		}

		/// <summary>
		/// Called when owner is set to the remote client.
		/// </summary>
		public void OwnerSetToRemote() {

		}

		/// <summary>
		/// Called when owner is removed.
		/// </summary>
		public void OwnerRemoved() {
			rigidbody.useGravity = true;
		}

		/// <summary>
		/// Called when sync control is taken by force.
		/// </summary>
		public void SyncTakenByForce() {
			if (holdingObject == true) {
				Logger.Log("Dropped object because remote player has taken control of it!");
				GamePlayer.Instance.DropStolenObject();
			}
		}

		/// <summary>
		/// Called when an object is constantly syncing. (Usually when a pickupable is picked up, or when a vehicle is being driven)
		/// </summary>
		/// <param name="newValue">If object is being constantly synced.</param>
		public void ConstantSyncChanged(bool newValue) {
			holdingObject = newValue;
			if (!holdingObject) {
				rigidbody.useGravity = true;
			}
		}
	}
}
