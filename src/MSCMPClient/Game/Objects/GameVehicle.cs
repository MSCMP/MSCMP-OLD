using HutongGames.PlayMaker;
using UnityEngine;

namespace MSCMP.Game.Objects {
	/// <summary>
	/// Representation of game vehicle.
	/// </summary>
	class GameVehicle {

		GameObject gameObject = null;

		CarDynamics dynamics = null;
		Drivetrain driveTrain = null;

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

		AxisCarController axisCarController = null;
		MPCarController mpCarController = null;

		public delegate void OnEnter();
		public delegate void OnLeave();
		public OnEnter onEnter = () => {
			Logger.Log("On Enter");
		};
		public OnLeave onLeave = () => {
			Logger.Log("On Leave");
		};

		public string Name {
			get {
				return gameObject != null ? gameObject.name : "";
			}
		}

		public Transform VehicleTransform {
			get {
				return gameObject.transform;
			}
		}

		public float Steering {
			get {
				return dynamics.carController.steering;
			}
			set {
				mpCarController.remoteSteerInput = value;
			}
		}

		public float ThrottleInput {
			get {
				return dynamics.carController.throttleInput;
			}
			set {
				mpCarController.throttleInput = value;
			}
		}
		public float BrakeInput {
			get {
				return dynamics.carController.brakeInput;
			}
			set {
				mpCarController.brakeInput = value;
			}
		}
		public float HandbrakeInput {
			get {
				return dynamics.carController.handbrakeInput;
			}
			set {
				mpCarController.handbrakeInput = value;
			}
		}
		public float ClutchInput {
			get {
				return dynamics.carController.clutchInput;
			}
			set {
				mpCarController.clutchInput = value;
			}
		}
		public bool StartEngineInput {
			get {
				return dynamics.carController.startEngineInput;
			}
			set {
				mpCarController.startEngineInput = value;
			}
		}
		public int Gear {
			get {
				return driveTrain.gear;
			}
			set {
				mpCarController.remoteTargetGear = value;
			}
		}


		GameObject seatGameObject = null;

		public Transform SeatTransform {
			get {
				return seatGameObject.transform;
			}
		}


		/// <summary>
		/// PlayMaker state action executed when local player enters vehicle.
		/// </summary>
		private class OnEnterAction : FsmStateAction {
			private GameVehicle vehicle;

			public OnEnterAction(GameVehicle veh) {
				vehicle = veh;
			}

			public override void OnEnter() {
				Utils.CallSafe("OnEnterHandler", () => {
					if (Fsm.PreviousActiveState != null && Fsm.PreviousActiveState.Name == "Death") {
						if (vehicle.onEnter != null) {
							vehicle.onEnter();
						}
					}
				});
				Finish();
			}
		}

		/// <summary>
		/// PlayMaker state action executed when local player leaves vehicle.
		/// </summary>
		private class OnLeaveAction : FsmStateAction {
			private GameVehicle vehicle;

			public OnLeaveAction(GameVehicle veh) {
				vehicle = veh;
			}

			public override void OnEnter() {
				Utils.CallSafe("OnLeaveHandler", () => {
					if (Fsm.PreviousActiveState != null && Fsm.PreviousActiveState.Name == "Create player") {
						if (vehicle.onLeave != null) {
							vehicle.onLeave();
						}
					}
				});
				Finish();
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="go">Vehicle game object.</param>
		public GameVehicle(GameObject go) {
			gameObject = go;

			dynamics = gameObject.GetComponent<CarDynamics>();
			Client.Assert(dynamics != null, "Missing car dynamics!");

			driveTrain = gameObject.GetComponent<Drivetrain>();

			axisCarController = gameObject.GetComponent<AxisCarController>();
			mpCarController = gameObject.AddComponent<MPCarController>();

			PlayMakerFSM[] fsms = gameObject.GetComponentsInChildren<PlayMakerFSM>();

			foreach (var fsm in fsms) {
				if (fsm.FsmName == "PlayerTrigger") {
					SetupPlayerTriggerHooks(fsm);

					// Temp - use player trigger..
					seatGameObject = fsm.gameObject;
				}
			}
		}

		/// <summary>
		/// Set remote steering state.
		/// </summary>
		public bool RemoteSteering {
			set {
				axisCarController.enabled = !value;
				mpCarController.enabled = value;
			}
			get {
				return axisCarController.enabled;
			}
		}

		/// <summary>
		/// Setup player trigger related hooks.
		/// </summary>
		/// <param name="fsm">The fsm to hook.</param>
		private void SetupPlayerTriggerHooks(PlayMakerFSM fsm) {
			FsmState playerInCarState = fsm.Fsm.GetState("Player in car");
			FsmState waitForPlayerState = fsm.Fsm.GetState("Wait for player");

			if (waitForPlayerState != null) {
				PlayMakerUtils.AddNewAction(waitForPlayerState, new OnLeaveAction(this));
			}

			if (playerInCarState != null) {
				PlayMakerUtils.AddNewAction(playerInCarState, new OnEnterAction(this));
			}
		}

		public void SetPosAndRot(Vector3 pos, Quaternion rot) {
			Transform transform = gameObject.transform;
			transform.position = pos;
			transform.rotation = rot;
		}

		public void UpdateIMGUI() {
			string vinfo = "Vehicle info:\n" +
				$"  Name: {gameObject.name}\n" +
				$"  Steering: {Steering}\n";

			Transform ignitionTransform = gameObject.transform.Find("LOD/Dashboard/Ignition");
			if (ignitionTransform != null) {
				GameObject ignition = ignitionTransform.gameObject;
				PlayMakerFSM use = Utils.GetPlaymakerScriptByName(ignition, "Use");
				if (use != null) {
					vinfo += "  > Use:\n";

					vinfo += "     Active state: " + use.Fsm.ActiveStateName + " \n";
					if (use.Fsm.PreviousActiveState != null) {
						vinfo += "     Prev Active state: " + use.Fsm.PreviousActiveState.Name + " \n";
					}

				}
				else {
					vinfo += "  > Use missing!\n";
				}
			}
			else {
				vinfo += "  > Ignition missing\n";
			}

			Transform starter = gameObject.transform.Find("Starter");
			if (starter != null) {
				vinfo += "  > Starter\n";

				PlayMakerFSM starterFsm = Utils.GetPlaymakerScriptByName(starter.gameObject, "Starter");
				vinfo += "     Active state: " + starterFsm.Fsm.ActiveStateName + " \n";
				if (starterFsm.Fsm.PreviousActiveState != null) {
					vinfo += "     Prev Active state: " + starterFsm.Fsm.PreviousActiveState.Name + " \n";
				}
			}


			GUI.Label(new Rect(10, 200, 500, 500), vinfo);
		}



	}
}
