using System;
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
		/// Returns variables to be sent to the remote client.
		/// </summary>
		/// <returns>Variables to be sent to the remote client.</returns>
		public float[] ReturnSyncedVariables() {
			return null;
		}

		/// <summary>
		/// Handle variables sent from the remote client.
		/// </summary>
		public void HandleSyncedVariables(float[] variables) {

		}

		/// <summary>
		/// Called when owner is set to the remote client.
		/// </summary>
		public void OwnerSetToRemote() {

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
		}
	}
}
