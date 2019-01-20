using System;
using UnityEngine;
using MSCMP.Game.Components;
using HutongGames.PlayMaker;

namespace MSCMP.Game.Objects {
	class AIVehicle : ISyncedObject {

		ObjectSyncComponent syncComponent;
		bool isSyncing = false;

		GameObject gameObject;
		Rigidbody rigidbody;

		GameObject parentGameObject;

		PlayMakerFSM throttleFsm;
		PlayMakerFSM navigationFsm;
		PlayMakerFSM directionFsm;

		CarDynamics dynamics;

		float isClockwise = 0;

		public enum VehicleTypes {
			Bus,
			Amis,
			Traffic,
			TrafficDirectional,
			Fitan,
		}

		public VehicleTypes type;

		class MPCarController : AxisCarController {
			public float remoteThrottleInput = 0.0f;
			public float remoteBrakeInput = 0.0f;
			public float remoteSteerInput = 0.0f;
			public float remoteHandbrakeInput = 0.0f;
			public float remoteClutchInput = 0.0f;
			public bool remoteStartEngineInput = false;
			public int remoteTargetGear = 0;

			protected override void GetInput(out float throttleInput, out float brakeInput, out float steerInput, out float handbrakeInput, out float clutchInput, out bool startEngineInput, out int targetGear) {
				throttleInput = remoteThrottleInput;
				brakeInput = remoteBrakeInput;
				steerInput = remoteSteerInput;
				handbrakeInput = remoteHandbrakeInput;
				clutchInput = remoteClutchInput;
				startEngineInput = remoteStartEngineInput;
				targetGear = remoteTargetGear;
			}
		}

		public float Steering {
			get {
				return dynamics.carController.steering;
			}
			set {
				dynamics.carController.steering = value;
			}
		}

		public float Throttle {
			get {
				return dynamics.carController.throttleInput;
			}
			set {
				dynamics.carController.throttleInput = value;
			}
		}

		public float Brake {
			get {
				return dynamics.carController.brakeInput;
			}
			set {
				dynamics.carController.brakeInput = value;
			}
		}

		public float TargetSpeed {
			get {
				return throttleFsm.FsmVariables.GetFsmFloat("TargetSpeed").Value;
			}
			set {
				remoteTargetSpeed = value;
			}
		}

		public int Waypoint {
			get {
				return Convert.ToInt32(navigationFsm.FsmVariables.GetFsmGameObject("Waypoint").Value.name);
			}
		}

		public GameObject WaypointSet {
			set {
				navigationFsm.FsmVariables.GetFsmGameObject("Waypoint").Value = value;
			}
		}

		public int Route {
			get {
				string route = navigationFsm.FsmVariables.GetFsmGameObject("Waypoint").Value.transform.parent.name;
				if (route == "BusRoute") {
					return 0;
				}
				else if (route == "DirtRoad") {
					return 1;
				}
				else if (route == "Highway") {
					return 2;
				}
				else if (route == "HomeRoad") {
					return 3;
				}
				else if (route == "RoadRace") {
					return 4;
				}
				else if (route == "Trackfield") {
					return 5;
				}
				else {
					return 6;
				}
			}
		}

		public int WaypointStart {
			get {
				return navigationFsm.FsmVariables.GetFsmInt("WaypointStart").Value;
			}
			set {
				navigationFsm.FsmVariables.GetFsmInt("WaypointStart").Value = value;
			}
		}

		public int WaypointEnd {
			get {
				return navigationFsm.FsmVariables.GetFsmInt("WaypointEnd").Value;
			}
			set {
				navigationFsm.FsmVariables.GetFsmInt("WaypointEnd").Value = value;
			}
		}

		float remoteTargetSpeed;

		float steamID = Steamworks.SteamUser.GetSteamID().m_SteamID;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="go"></param>
		public AIVehicle(GameObject go, ObjectSyncComponent osc) {
			gameObject = go;
			syncComponent = osc;
			parentGameObject = go.transform.parent.gameObject;

			// Set vehicle type, used to apply vehicle-specific event hooks.
			string goName = gameObject.transform.parent.gameObject.name;
			if (goName == "AMIS2" || goName == "KYLAJANI") {
				type = VehicleTypes.Amis;
			}
			else if (goName == "BUS") {
				type = VehicleTypes.Bus;
			}
			else if (goName == "FITTAN" && parentGameObject.transform.FindChild("Navigation") != null) {
				type = VehicleTypes.Fitan;
			}
			else if (parentGameObject.transform.FindChild("NavigationCW") != null || parentGameObject.transform.FindChild("NavigationCCW") != null) {
				type = VehicleTypes.TrafficDirectional;
			}
			else {
				type = VehicleTypes.Traffic;
			}

			rigidbody = parentGameObject.GetComponent<Rigidbody>();

			dynamics = parentGameObject.GetComponent<CarDynamics>();

			throttleFsm = Utils.GetPlaymakerScriptByName(parentGameObject, "Throttle");

			if (type == VehicleTypes.TrafficDirectional) {
				if (parentGameObject.transform.FindChild("NavigationCW") != null) {
					navigationFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("NavigationCW").gameObject, "Navigation");
					isClockwise = 1;
				}
				else {
					navigationFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("NavigationCCW").gameObject, "Navigation");
					isClockwise = 0;
				}
				directionFsm = Utils.GetPlaymakerScriptByName(parentGameObject, "Direction");
			}
			else {
				navigationFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("Navigation").gameObject, "Navigation");
			}

			EventHooks();
		}

		/// <summary>
		/// Get object's Transform.
		/// </summary>
		/// <returns>Object's Transform.</returns>
		public Transform ObjectTransform() {
			return parentGameObject.transform;
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
				isSyncing = true;
				return true;
			}
			else {
				isSyncing = false;
				return false;
			}
		}

		/// <summary>
		/// Called when a player enters range of an object.
		/// </summary>
		/// <returns>True if the player should tkae ownership of the object.</returns>
		public bool ShouldTakeOwnership() {
			return true;
		}

		/// <summary>
		/// Returns variables to be sent to the remote client.
		/// </summary>
		/// <returns>Variables to be sent to the remote client.</returns>
		public float[] ReturnSyncedVariables(bool sendAllVariables) {
			if (isSyncing == true) {
				float[] variables = { Steering, Throttle, Brake, TargetSpeed, Waypoint, Route, WaypointStart, WaypointEnd, isClockwise };
				return variables;
			}
			else {
				return null;
			}
		}

		/// <summary>
		/// Handle variables sent from the remote client.
		/// </summary>
		public void HandleSyncedVariables(float[] variables) {
			if (variables != null) {
				Steering = variables[0];
				Throttle = variables[1];
				Brake = variables[2];
				TargetSpeed = variables[3];
				WaypointSet = TrafficManager.GetWaypoint(variables[4], (int)variables[5]);
				WaypointStart = Convert.ToInt32(variables[6]);
				WaypointEnd = Convert.ToInt32(variables[7]);
				if (isClockwise != variables[8]) {
					isClockwise = variables[8];
					if (isClockwise == 1) {
						navigationFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("NavigationCW").gameObject, "Navigation");
					}
					else {
						navigationFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("NavigationCCW").gameObject, "Navigation");
					}
				}
			}
		}

		/// <summary>
		/// Called when sync control is taken by force.
		/// </summary>
		public void SyncTakenByForce() {

		}

		/// <summary>
		/// Called when owner is set to the remote client.
		/// </summary>
		public void OwnerSetToRemote() {
			gameObject.SetActive(true);
		}

		/// <summary>
		/// Called when owner is removed.
		/// </summary>
		public void OwnerRemoved() {

		}

		/// <summary>
		/// Called when an object is constantly syncing. (Usually when a pickupable is picked up, or when a vehicle is being driven)
		/// </summary>
		/// <param name="newValue">If object is being constantly synced.</param>
		public void ConstantSyncChanged(bool newValue) {

		}

		// Event hooks
		public void EventHooks() {
			// Generic vehicle FSMs.
			throttleFsm = Utils.GetPlaymakerScriptByName(parentGameObject, "Throttle");
			EventHook.SyncAllEvents(throttleFsm, new Func<bool>(() => {
				if (syncComponent.Owner != steamID && syncComponent.Owner != 0 || syncComponent.Owner == 0 && !Network.NetManager.Instance.IsHost) {
					return true;
				}
				return false;
			}));

			// Traffic FSMs.
			EventHook.AddWithSync(directionFsm, "CW", new Func<bool>(() => {
				isClockwise = 1;
				return false;
			}));
			EventHook.AddWithSync(directionFsm, "CCW", new Func<bool>(() => {
				isClockwise = 0;
				return false;
			}));

			// Bus specific FSMs.
			if (type == VehicleTypes.Bus) {
				PlayMakerFSM doorFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("Route").gameObject, "Door");
				PlayMakerFSM startFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("Route").gameObject, "Start");

				EventHook.SyncAllEvents(doorFsm, new Func<bool>(() => {
					if (syncComponent.Owner != steamID && syncComponent.Owner != 0 || syncComponent.Owner == 0 && !Network.NetManager.Instance.IsHost) {
						return true;
					}
					return false;
				}));

				EventHook.SyncAllEvents(startFsm, new Func<bool>(() => {
					if (syncComponent.Owner != steamID && syncComponent.Owner != 0 || syncComponent.Owner == 0 && !Network.NetManager.Instance.IsHost) {
						return true;
					}
					return false;
				}));
			}

			// None traffic cars specific FSMs.
			if (type == VehicleTypes.Amis || type == VehicleTypes.Fitan) {
				PlayMakerFSM crashFsm = Utils.GetPlaymakerScriptByName(parentGameObject.transform.FindChild("CrashEvent").gameObject, "Crash");

				EventHook.SyncAllEvents(crashFsm, new Func<bool>(() => {
					if (syncComponent.Owner != steamID && syncComponent.Owner != 0 || syncComponent.Owner == 0 && !Network.NetManager.Instance.IsHost) {
						return true;
					}
					return false;
				}));
			}

			// Sync vehicle data with the host on spawn.
			if (Network.NetManager.Instance.IsOnline && !Network.NetManager.Instance.IsHost) {
				syncComponent.RequestObjectSync();
			}
		}
	}
}
