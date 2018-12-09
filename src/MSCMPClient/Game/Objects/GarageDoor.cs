using System;
using UnityEngine;

/// <summary>
/// Handles sync for the garage doors.
/// </summary>
namespace MSCMP.Game.Objects {
	class GarageDoor : ISyncedObject {
		GameObject gameObject;
		Rigidbody rigidbody;

		float lastRotation;

		/// <summary>
		/// Constructor.
		/// </summary>
		public GarageDoor(GameObject go) {
			gameObject = go.transform.parent.gameObject;
			rigidbody = gameObject.GetComponent<Rigidbody>();
			lastRotation = gameObject.transform.localRotation.z;

			HookEvents(go);
		}

		/// <summary>
		/// Hook events.
		/// </summary>
		void HookEvents(GameObject go) {
			PlayMakerFSM doorFSM = Utils.GetPlaymakerScriptByName(go, "Use");
			EventHook.Add(doorFSM, "Open", new Func<bool>(() => {
				go.GetComponent<Components.ObjectSyncComponent>().TakeSyncControl();
				return false;
			}));
			EventHook.Add(doorFSM, "Close", new Func<bool>(() => {
				go.GetComponent<Components.ObjectSyncComponent>().TakeSyncControl();
				return false;
			}));
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
			if ((lastRotation - gameObject.transform.localRotation.z) > 0.005 || (lastRotation - gameObject.transform.localRotation.z) < -0.005) {
				lastRotation = gameObject.transform.localRotation.z;
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
		public float[] ReturnSyncedVariables(bool sendAllVariables) {
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
		/// Called when owner is removed.
		/// </summary>
		public void OwnerRemoved() {

		}

		/// <summary>
		/// Called when sync control is taken by force.
		/// </summary>
		public void SyncTakenByForce() {

		}

		/// <summary>
		/// Called when an object is constantly syncing. (Usually when a pickupable is picked up, or when a vehicle is being driven)
		/// </summary>
		/// <param name="newValue">If object is being constantly synced.</param>
		public void ConstantSyncChanged(bool newValue) {

		}
	}
}
