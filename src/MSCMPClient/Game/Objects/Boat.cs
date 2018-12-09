using System;
using UnityEngine;
using HutongGames.PlayMaker;

/// <summary>
/// Handles syncing and events of the boat.
/// </summary>
namespace MSCMP.Game.Objects {
	class Boat : ISyncedObject {
		GameObject gameObject;
		GameObject boatGO;
		Rigidbody rigidbody;

		PlayMakerFSM jankFSM;
		PlayMakerFSM ignitionFSM;
		PlayMakerFSM shutOffFSM;

		PlayMakerFSM throttleSteerFSM;
		PlayMakerFSM engineFSM;
		GameObject engineGO;

		PlayMakerFSM gearFSM;

		GameObject motorGO;

		PlayMakerFSM driveFSM;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="go">Boat GameObject.</param>
		public Boat(GameObject go) {
			gameObject = go;
			boatGO = go.transform.parent.parent.parent.gameObject;
			rigidbody = boatGO.GetComponent<Rigidbody>();

			PlayMakerFSM[] fsms = boatGO.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.Fsm.Name == "Jank" && fsm.gameObject.name == "Ignition") {
					jankFSM = fsm;
				}
				else if (fsm.Fsm.Name == "Use" && fsm.gameObject.name == "Ignition") {
					ignitionFSM = fsm;
				}
				else if (fsm.Fsm.Name == "Use" && fsm.gameObject.name == "ShutOff") {
					shutOffFSM = fsm;
				}
				else if (fsm.Fsm.Name == "Use" && fsm.gameObject.name == "Gear") {
					gearFSM = fsm;
				}
				else if (fsm.Fsm.Name == "PlayerTrigger" && fsm.gameObject.name == "DriveTrigger") {
					driveFSM = fsm;
				}
			}

			PlayMakerFSM[] allFsms = Resources.FindObjectsOfTypeAll<PlayMakerFSM>();
			bool foundOther = false;
			foreach (PlayMakerFSM fsm in allFsms) {
				if (fsm.Fsm.Name == "ThrottleSteer" && fsm.gameObject.name == "Controls") {
					throttleSteerFSM = fsm;
					if (foundOther) {
						break;
					}
					foundOther = true;
				}
				else if (fsm.Fsm.Name == "Simulation" && fsm.gameObject.name == "Engine") {
					engineFSM = fsm;
					engineGO = fsm.gameObject;
					if (foundOther) {
						break;
					}
					foundOther = true;
				}
			}

			motorGO = boatGO.transform.FindChild("GFX").FindChild("Motor").FindChild("Pivot").gameObject;

			HookEvents();
		}

		/// <summary>
		/// Hook events related to the boat.
		/// </summary>
		void HookEvents() {
			// J A N K - Yes, it's called that.
			EventHook.AddWithSync(jankFSM, "State 1");
			EventHook.AddWithSync(jankFSM, "Fail");
			EventHook.AddWithSync(jankFSM, "Start");

			// Ignition
			EventHook.AddWithSync(ignitionFSM, "State 1");

			// Shut Off
			EventHook.AddWithSync(shutOffFSM, "Shut Off");

			// Gears
			EventHook.AddWithSync(gearFSM, "First");
			EventHook.AddWithSync(gearFSM, "Neutral");
			EventHook.AddWithSync(gearFSM, "Reverse");

			// Enter as driver
			EventHook.Add(driveFSM, "Player in car", new Func<bool>(() => {
				gameObject.GetComponent<Components.ObjectSyncComponent>().TakeSyncControl();
				return false;
			}));
		}

		/// <summary>
		/// Get object's Transform.
		/// </summary>
		/// <returns>Object's Transform.</returns>
		public Transform ObjectTransform() {
			return boatGO.transform;
		}

		/// <summary>
		/// Check is periodic sync of the object is enabled.
		/// </summary>
		/// <returns>Periodic sync enabled or disabled.</returns>
		public bool PeriodicSyncEnabled() {
			return true;
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
		public float[] ReturnSyncedVariables(bool sendAllVariables) {
			if (!engineGO.activeSelf) {
				engineGO.SetActive(true);
			}
			float[] variables = {
				engineFSM.FsmVariables.GetFsmFloat("Throttle").Value,
				engineFSM.FsmVariables.GetFsmFloat("RPMmax").Value,
				motorGO.transform.localRotation.y
			};
			return variables;
		}

		/// <summary>
		/// Handle variables sent from the remote client.
		/// </summary>
		public void HandleSyncedVariables(float[] variables) {
			if (!engineGO.activeSelf) {
				engineGO.SetActive(true);
			}
			engineFSM.FsmVariables.GetFsmFloat("Throttle").Value = variables[0];
			engineFSM.FsmVariables.GetFsmFloat("RPMmax").Value = variables[1];
			motorGO.transform.localRotation = new Quaternion(motorGO.transform.localRotation.x, variables[2], motorGO.transform.localRotation.z, motorGO.transform.localRotation.w);
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
