using HutongGames.PlayMaker;
using UnityEngine;
using System;

namespace MSCMP.Game.Objects {
	/// <summary>
	/// Representation of game vehicle.
	/// </summary>
	class GameVehicle {

		GameObject gameObject = null;
		public GameObject GameObject {
			get { return gameObject; }
		}

		CarDynamics dynamics = null;
		Drivetrain driveTrain = null;

		bool isDriver = false;

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

		public delegate void OnEnter(bool passenger);
		public delegate void OnLeave();
		public delegate void OnEngineStateChanged(EngineStates state, DashboardStates dashstate, float startTime);
		public delegate void OnVehicleSwitchChanged(SwitchIDs id, bool newValue, float newValueFloat);
		public OnEnter onEnter = (bool passenger) => {
			Logger.Log("On Enter");
		};
		public OnLeave onLeave = () => {
			Logger.Log("On Leave");
		};
		public OnEngineStateChanged onEngineStateChanged = (EngineStates state, DashboardStates dashstate, float startTime) => {
			Logger.Debug($"Engine state changed to: {state.ToString()}");
		};
		public OnVehicleSwitchChanged onVehicleSwitchChanges = (SwitchIDs id, bool newValue, float newValueFloat) => {
			Logger.Debug($"Switch {id.ToString()} changed to: {newValue} (Float: {newValueFloat})");
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

		public float Throttle {
			get {
				return dynamics.carController.throttleInput;
			}
			set {
				mpCarController.remoteThrottleInput = value;
			}
		}

		public float Brake {
			get {
				return dynamics.carController.brakeInput;
			}
			set {
				mpCarController.remoteBrakeInput = value;
			}
		}

		public float ClutchInput {
			get {
				return driveTrain.clutch.GetClutchPosition();
			}
			set {
				driveTrain.clutch.SetClutchPosition(value);
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

		public bool Range {
			get {
				return gearIndicatorFsm.Fsm.GetFsmBool("Range").Value;
			}
			set {
				if (hasRange == true) {
					rangeFsm.SendEvent(MP_RANGE_SWITCH_EVENT_NAME);
				}
			}
		}

		public float Fuel {
			get {
				return fuelTankFsm.Fsm.GetFsmFloat("FuelLevel").Value;
			}
			set {
				fuelTankFsm.Fsm.GetFsmFloat("FuelLevel").Value = value;
			}
		}

		public float FrontHydraulic {
			get {
				return frontHydraulicFsm.Fsm.GetFsmFloat("LeverPos").Value;
			}
			set {
				frontHydraulicFsm.Fsm.GetFsmFloat("LeverPos").Value = value;
			}
		}


		GameObject seatGameObject = null;
		GameObject pSeatGameObject = null;
		GameObject starterGameObject = null;

		// General
		PlayMakerFSM starterFsm = null;
		PlayMakerFSM handbrakeFsm = null;
		PlayMakerFSM fuelTankFsm = null;
		PlayMakerFSM rangeFsm = null;
		PlayMakerFSM gearIndicatorFsm = null;
		PlayMakerFSM dashboardFsm = null;
		PlayMakerFSM fuelTapFsm = null;
		PlayMakerFSM lightsFsm = null;
		PlayMakerFSM wipersFsm = null;
		PlayMakerFSM interiorLightFsm = null;
		PlayMakerFSM frontHydraulicFsm = null;
		PlayMakerFSM indicatorsFsm = null;

		// Truck specific
		PlayMakerFSM hydraulicPumpFsm = null;
		PlayMakerFSM diffLockFsm = null;
		PlayMakerFSM axleLiftFsm = null;
		PlayMakerFSM spillValveFsm = null;

		// Misc
		PlayMakerFSM waspNestFsm = null;

		public bool hasRange = false;
		bool hasLeverParkingBrake = false;
		bool hasPushParkingBrake = false;

		bool isTruck = false;
		public bool isTractor = false;

		bool hydraulicPumpFirstRun = true;
		bool diffLockFirstRun = true;
		bool axleLiftFirstRun = true;

		public Transform SeatTransform {
			get {
				return seatGameObject.transform;
			}
		}

		public Transform PassengerSeatTransform {
			get {
				return pSeatGameObject.transform;
			}
		}

		public enum EngineStates {
			WaitForStart,
			ACC,
			Glowplug,
			TurnKey,
			CheckClutch,
			StartingEngine,
			StartEngine,
			StartOrNot,
			MotorRunning,
			Wait,
			Null,
		}

		public enum DashboardStates {
			ACCon,
			Test,
			ACCon2,
			MotorStarting,
			ShutOff,
			MotorOff,
			WaitButton,
			WaitPlayer,
			Null,
		}

		public enum SwitchIDs {
			HandbrakePull,
			HandbrakeLever,
			Lights,
			Wipers,
			HydraulicPump,
			DiffLock,
			AxleLift,
			InteriorLight,
			SpillValve,
			FuelTap,
			TractorHydraulics,
			DestroyWaspNest,
			FlatbedHatch,
		}

		// Dashboard
		string MP_ACC_ON_EVENT_NAME = "MPACCON";
		string MP_TEST_EVENT_NAME = "MPTEST";
		string MP_ACC_ON_2_EVENT_NAME = "MPACCON2";
		string MP_MOTOR_STARTING_EVENT_NAME = "MPMOTORSTARTING";
		string MP_SHUT_OFF_EVENT_NAME = "MPSHUTOFF";
		string MP_MOTOR_OFF_EVENT_NAME = "MPMOTOROFF";
		string MP_WAIT_BUTTON_EVENT_NAME = "MPWAITBUTTON";
		string MP_WAIT_PLAYER_EVENT_NAME = "MPWAITPLAYER";

		// Misc
		string MP_RANGE_SWITCH_EVENT_NAME = "MPRANGE";

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
							vehicle.onEnter(false);
							vehicle.isDriver = true;

							if (vehicle.driveTrain != null) {
								vehicle.driveTrain.canStall = false;
							}
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
							vehicle.isDriver = false;

							if (vehicle.driveTrain != null) {
								vehicle.driveTrain.canStall = false;
							}
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

			if (driveTrain != null) {
				driveTrain.canStall = false;
			}

			axisCarController = gameObject.GetComponent<AxisCarController>();
			mpCarController = gameObject.AddComponent<MPCarController>();

			// Used for creating truck-specific events
			if (go.name.StartsWith("GIFU")) {
				isTruck = true;
			}

			PlayMakerFSM[] fsms = gameObject.GetComponentsInChildren<PlayMakerFSM>();

			foreach (var fsm in fsms) {
				if (fsm.FsmName == "PlayerTrigger") {
					SetupPlayerTriggerHooks(fsm);

					// Temp - use player trigger. (No idea what this comment meant, it's now many months later. :P)
					seatGameObject = fsm.gameObject;

					// Add Passenger seat.
					if (seatGameObject.name == "DriveTrigger" && !gameObject.name.StartsWith("JONNEZ") && !gameObject.name.StartsWith("KEKMET")) {
						GameObject passengerSeat = GameObject.CreatePrimitive(PrimitiveType.Cube);
						pSeatGameObject = passengerSeat;

						passengerSeat.transform.parent = fsm.gameObject.transform.parent;
						passengerSeat.transform.position = passengerSeat.transform.parent.position;
						passengerSeat.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);

						passengerSeat.transform.GetComponent<BoxCollider>().isTrigger = true;

						PassengerSeat pSeatScript = passengerSeat.AddComponent(typeof(PassengerSeat)) as PassengerSeat;
						pSeatScript.VehicleType = gameObject.name;
						pSeatScript.DriversSeat = fsm.gameObject;

						pSeatScript.onEnter = () => {
							this.onEnter(true);
						};
						pSeatScript.onLeave = () => {
							this.onLeave();
						};
					}
				}

				// Starter
				else if (fsm.FsmName == "Starter") {
					starterGameObject = fsm.gameObject;
					starterFsm = fsm;
				}

				// Handbrake for Van, Ferndale, Tractor, Ruscko
				else if (fsm.gameObject.name == "ParkingBrake" && fsm.FsmName == "Use") {
					handbrakeFsm = fsm;
					hasPushParkingBrake = true;
				}

				// Handbrake for Truck
				else if (fsm.gameObject.name == "Parking Brake" && fsm.FsmName == "Use") {
					handbrakeFsm = fsm;
					hasLeverParkingBrake = true;
				}

				// Range selector
				else if (fsm.gameObject.name == "Range" && fsm.FsmName == "Use") {
					rangeFsm = fsm;
					hasRange = true;
				}

				// Fuel tank
				else if (fsm.gameObject.name == "FuelTank" && fsm.FsmName == "Data") {
					fuelTankFsm = fsm;
				}

				// Dashboard
				else if (fsm.gameObject.name == "Ignition" && fsm.FsmName == "Use") {
					dashboardFsm = fsm;
				}

				// Fuel tap
				else if (fsm.gameObject.name == "FuelTap" && fsm.FsmName == "Use") {
					fuelTapFsm = fsm;
				}

				// Lights
				else if (fsm.gameObject.name == "Lights" && fsm.FsmName == "Use" || fsm.gameObject.name == "ButtonLights" && fsm.FsmName == "Use" || fsm.gameObject.name == "knob" && fsm.FsmName == "Use") {
					lightsFsm = fsm;
				}

				// Wipers
				else if (fsm.gameObject.name == "Wipers" && fsm.FsmName == "Use" || fsm.gameObject.name == "ButtonWipers" && fsm.FsmName == "Use") {
					wipersFsm = fsm;
				}

				// Interior light
				else if (fsm.gameObject.name == "ButtonInteriorLight" && fsm.FsmName == "Use") {
					interiorLightFsm = fsm;
				}

				// Gear indicator - Used to get Range position
				else if (fsm.FsmName == "GearIndicator") {
					gearIndicatorFsm = fsm;
				}

				// Tractor front hydraulic
				else if (fsm.gameObject.name == "FrontHyd" && fsm.FsmName == "Use") {
					frontHydraulicFsm = fsm;
					isTractor = true;
				}

				// Wasp nest
				else if (fsm.gameObject.name == "WaspHive" && fsm.FsmName == "Data") {
					waspNestFsm = fsm;
				}

				// Indicators
				else if (fsm.gameObject.name == "TurnSignals" && fsm.FsmName == "Usage") {
					indicatorsFsm = fsm;
				}

				// Truck specific FSMs
				if (isTruck == true) {

					// Hydraulic pump
					if (fsm.gameObject.name == "Hydraulics" && fsm.FsmName == "Use") {
						hydraulicPumpFsm = fsm;
					}

					// Diff lock
					if (fsm.gameObject.name == "Differential lock" && fsm.FsmName == "Use") {
						diffLockFsm = fsm;
					}

					// Axle lift
					if (fsm.gameObject.name == "Liftaxle" && fsm.FsmName == "Use") {
						axleLiftFsm = fsm;
					}

					// Spill valve
					if (fsm.gameObject.name == "OpenSpill" && fsm.FsmName == "Use") {
						spillValveFsm = fsm;
					}
				}
			}

			if (starterFsm != null && dashboardFsm != null) {
				SetupVehicleHooks();
			}
		}

		public void SetRemoteSteering(bool enabled) {
			axisCarController.enabled = !enabled;
			mpCarController.enabled = enabled;
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

		/// <summary>
		/// Setup vehicle event hooks.
		/// </summary>
		private void SetupVehicleHooks() {
			FsmState pBrakeIncreaseState = null;
			FsmState pBrakeDecreaseState = null;
			if (hasPushParkingBrake == true) {
				pBrakeIncreaseState = handbrakeFsm.Fsm.GetState("INCREASE");
				pBrakeDecreaseState = handbrakeFsm.Fsm.GetState("DECREASE");
			}

			FsmState truckPBrakeFlipState = null;
			if (hasLeverParkingBrake == true) {
				truckPBrakeFlipState = handbrakeFsm.Fsm.GetState("Flip");
			}

			FsmState rangeSwitchState = null;
			if (hasRange == true) {
				if (isTruck == true) {
					rangeSwitchState = rangeFsm.Fsm.GetState("Switch");
				}
				else if (isTractor == true) {
					rangeSwitchState = rangeFsm.Fsm.GetState("Flip");
				}
			}

			FsmState fuelTapState = null;
			if (fuelTapFsm != null) {
				fuelTapState = fuelTapFsm.Fsm.GetState("Test");
			}

			FsmState lightsState = null;
			if (lightsFsm != null) {
				if (isTruck == true) {
					lightsState = lightsFsm.Fsm.GetState("Sound 2");
				}
				else {
					lightsState = lightsFsm.Fsm.GetState("Sound");
				}
			}

			FsmState wipersState = null;
			if (wipersFsm != null) {
				wipersState = wipersFsm.Fsm.GetState("Test 2");
			}

			FsmState interiorLightState = null;
			if (interiorLightFsm != null) {
				interiorLightState = interiorLightFsm.Fsm.GetState("Switch");
			}

			FsmState hydraulicPumpState = null;
			FsmState diffLockState = null;
			FsmState axleLiftState = null;
			FsmState spillValveState = null;
			if (isTruck == true) {
				hydraulicPumpState = hydraulicPumpFsm.Fsm.GetState("Test");
				diffLockState = diffLockFsm.Fsm.GetState("Test");
				axleLiftState = axleLiftFsm.Fsm.GetState("Test");
				spillValveState = spillValveFsm.Fsm.GetState("Switch");
			}

			FsmState waspNestDestroyState = null;
			if (waspNestFsm != null) {
				waspNestDestroyState = waspNestFsm.Fsm.GetState("State 2");
			}

			//Engine states

			string[] eventNames = { "Wait for start" };

			foreach (string eventName in eventNames) {

			}

			if (starterFsm.Fsm.GetState("Wait for start") != null) {
				EventHook.Add(starterFsm, "Wait for start", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition != null) {
						if (starterFsm.Fsm.LastTransition.EventName == "MP_Wait for start" || this.isDriver == false) {
							return true;
						}
					}

					this.onEngineStateChanged(EngineStates.WaitForStart, DashboardStates.MotorOff, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("ACC") != null) {
				EventHook.Add(starterFsm, "ACC", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_ACC" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.ACC, DashboardStates.Test, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Turn key") != null) {
				EventHook.Add(starterFsm, "Turn key", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Turn key" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.TurnKey, DashboardStates.ACCon2, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Check clutch") != null) {
				EventHook.Add(starterFsm, "Check clutch", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Check clutch" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.CheckClutch, DashboardStates.Null, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Starting engine") != null) {
				EventHook.Add(starterFsm, "Starting engine", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Starting engine" || this.isDriver == false) {
						return true;
					}

					float startTime = this.starterFsm.Fsm.GetFsmFloat("StartTime").Value;

					this.onEngineStateChanged(EngineStates.StartingEngine, DashboardStates.MotorStarting, startTime);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Start engine") != null) {
				EventHook.Add(starterFsm, "Start engine", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Start engine" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.StartEngine, DashboardStates.Null, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Wait") != null) {
				EventHook.Add(starterFsm, "Wait", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Wait" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.Wait, DashboardStates.Null, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Start or not") != null) {
				EventHook.Add(starterFsm, "Start or not", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Start or not" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.StartOrNot, DashboardStates.Null, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("Motor running") != null) {
				EventHook.Add(starterFsm, "Motor running", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_Motor running" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.MotorRunning, DashboardStates.WaitPlayer, -1);
					return false;
				}));
			}

			if (starterFsm.Fsm.GetState("ACC / Glowplug") != null) {
				EventHook.Add(starterFsm, "ACC / Glowplug", new Func<bool>(() => {
					if (starterFsm.Fsm.LastTransition.EventName == "MP_ACC / Glowplug" || this.isDriver == false) {
						return true;
					}

					this.onEngineStateChanged(EngineStates.Glowplug, DashboardStates.Null, -1);
					return false;
				}));
			}

			// Dashboard
			if (dashboardFsm.Fsm.GetState("ACC on") != null) {
				FsmEvent mpAccOnState = dashboardFsm.Fsm.GetEvent(MP_ACC_ON_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpAccOnState, "ACC on");
			}

			if (dashboardFsm.Fsm.GetState("Test") != null) {
				FsmEvent mpTestState = dashboardFsm.Fsm.GetEvent(MP_TEST_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpTestState, "Test");
			}

			if (dashboardFsm.Fsm.GetState("ACC on 2") != null) {
				FsmEvent mpAccOn2State = dashboardFsm.Fsm.GetEvent(MP_ACC_ON_2_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpAccOn2State, "ACC on 2");
			}

			if (dashboardFsm.Fsm.GetState("Motor starting") != null) {
				FsmEvent mpMotorStartingState = dashboardFsm.Fsm.GetEvent(MP_MOTOR_STARTING_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpMotorStartingState, "Motor starting");
			}

			if (dashboardFsm.Fsm.GetState("Shut off") != null) {
				FsmEvent mpShutOffState = dashboardFsm.Fsm.GetEvent(MP_SHUT_OFF_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpShutOffState, "Shut off");
			}

			if (dashboardFsm.Fsm.GetState("Motor OFF") != null) {
				FsmEvent mpMotorOffState = dashboardFsm.Fsm.GetEvent(MP_MOTOR_OFF_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpMotorOffState, "Motor OFF");
			}

			if (dashboardFsm.Fsm.GetState("Wait button") != null) {
				FsmEvent mpWaitButtonState = dashboardFsm.Fsm.GetEvent(MP_WAIT_BUTTON_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpWaitButtonState, "Wait button");
			}

			if (dashboardFsm.Fsm.GetState("Wait player") != null) {
				FsmEvent mpWaitPlayerState = dashboardFsm.Fsm.GetEvent(MP_WAIT_PLAYER_EVENT_NAME);
				PlayMakerUtils.AddNewGlobalTransition(dashboardFsm, mpWaitPlayerState, "Wait player");
			}

			// Parking brake
			if (pBrakeDecreaseState != null) {
				EventHook.Add(handbrakeFsm, "DECREASE", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.HandbrakePull, false, this.handbrakeFsm.Fsm.GetFsmFloat("KnobPos").Value);
					return false;
				}), actionOnExit: true);
			}

			if (pBrakeIncreaseState != null) {
				EventHook.Add(handbrakeFsm, "INCREASE", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.HandbrakePull, false, this.handbrakeFsm.Fsm.GetFsmFloat("KnobPos").Value);
					return false;
				}), actionOnExit: true);
			}

			// Truck parking brake
			if (truckPBrakeFlipState != null) {
				EventHook.Add(handbrakeFsm, "Flip", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.HandbrakeLever, !this.handbrakeFsm.Fsm.GetFsmBool("Brake").Value, -1);
					return false;
				}));
			}

			// Range selector
			if (rangeSwitchState != null) {
				FsmEvent mpRangeSwitchState = rangeFsm.Fsm.GetEvent("MP_Range");
				if (isTractor == true) {
					PlayMakerUtils.AddNewGlobalTransition(rangeFsm, mpRangeSwitchState, "Flip");
				}
				else if (isTruck == true) {
					PlayMakerUtils.AddNewGlobalTransition(rangeFsm, mpRangeSwitchState, "Switch");
				}
			}

			// Fuel tap
			if (fuelTapState != null) {
				EventHook.Add(fuelTapFsm, "Test", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.FuelTap, !this.fuelTapFsm.Fsm.GetFsmBool("FuelOn").Value, -1);
					return false;
				}));
			}

			// Lights
			if (lightsFsm != null) {
				EventHook.AddWithSync(lightsFsm, "Off");
				EventHook.AddWithSync(lightsFsm, "Shorts");
				EventHook.AddWithSync(lightsFsm, "Longs");
			}

			// Indicators
			if (indicatorsFsm != null) {
				EventHook.AddWithSync(indicatorsFsm, "Activate dash");
				EventHook.AddWithSync(indicatorsFsm, "Activate dash 2");
				EventHook.AddWithSync(indicatorsFsm, "On", action: new Func<bool>(() => {
					if (this.isDriver == false) {
						return true;
					}
					return false;
				}));
				EventHook.AddWithSync(indicatorsFsm, "On 2", action: new Func<bool>(() => {
					if (this.isDriver == false) {
						return true;
					}
					return false;
				}));
				EventHook.AddWithSync(indicatorsFsm, "Off", action: new Func<bool>(() => {
					if (this.isDriver == false) {
						return true;
					}
					return false;
				}));
				EventHook.AddWithSync(indicatorsFsm, "Off 2", action: new Func<bool>(() => {
					if (this.isDriver == false) {
						return true;
					}
					return false;
				}));

				EventHook.AddWithSync(indicatorsFsm, "State 3", action: new Func<bool>(() => {
					if (this.isDriver == false) {
						Logger.Log("Turning off indicators!");
						GameObject left;
						GameObject right;
						left = this.gameObject.transform.FindChild("LOD/Electricity/PowerON/Blinkers/Left").gameObject;
						right = this.gameObject.transform.FindChild("LOD/Electricity/PowerON/Blinkers/Right").gameObject;
						// Ferndale has a different hierarchy. Why not, right?
						if (left == null) {
							left = this.gameObject.transform.FindChild("LOD/Electricity 1/PowerON/Blinkers/Left").gameObject;
							right = this.gameObject.transform.FindChild("LOD/Electricity 1/PowerON/Blinkers/Right").gameObject;
						}
						left.SetActive(false);
						right.SetActive(false);
						if (left == null) {
							Logger.Log("Left indicator not found!");
						}
					}
					return false;
				}));
			}

			// Wipers
			if (wipersState != null) {
				EventHook.Add(wipersFsm, "Test 2", new Func<bool>(() => {
					int selection = this.wipersFsm.Fsm.GetFsmInt("Selection").Value;
					if (selection == 2) {
						selection = 0;
					}
					else {
						selection++;
					}

					this.onVehicleSwitchChanges(SwitchIDs.Wipers, false, selection);
					return false;
				}));
			}

			// Interior light
			if (interiorLightState != null) {
				EventHook.Add(interiorLightFsm, "Switch", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.InteriorLight, !this.interiorLightFsm.Fsm.GetFsmBool("On").Value, -1);
					return false;
				}));
			}

			// Hydraulic pump
			if (hydraulicPumpState != null) {
				EventHook.Add(hydraulicPumpFsm, "Test", new Func<bool>(() => {
					if (this.hydraulicPumpFirstRun == false) {
						this.onVehicleSwitchChanges(SwitchIDs.HydraulicPump, !this.hydraulicPumpFsm.Fsm.GetFsmBool("On").Value, -1);
					}
					else {
						this.hydraulicPumpFirstRun = false;
					}
					return false;
				}));
			}

			// Spill valve
			if (spillValveState != null) {
				EventHook.Add(spillValveFsm, "Switch", new Func<bool>(() => {
					this.onVehicleSwitchChanges(SwitchIDs.SpillValve, !this.spillValveFsm.Fsm.GetFsmBool("Open").Value, -1);
					return false;
				}));
			}

			// Axle lift
			if (axleLiftState != null) {
				EventHook.Add(axleLiftFsm, "Test", new Func<bool>(() => {
					if (this.axleLiftFirstRun == false) {
						this.onVehicleSwitchChanges(SwitchIDs.AxleLift, !this.axleLiftFsm.Fsm.GetFsmBool("Up").Value, -1);
					}
					else {
						this.axleLiftFirstRun = false;
					}
					return false;
				}));
			}

			// Diff lock
			if (diffLockState != null) {
				EventHook.Add(diffLockFsm, "Test", new Func<bool>(() => {
					if (this.diffLockFirstRun == false) {
						this.onVehicleSwitchChanges(SwitchIDs.DiffLock, !this.diffLockFsm.Fsm.GetFsmBool("Lock").Value, -1);
					}
					else {
						this.diffLockFirstRun = false;
					}
					return false;
				}));
			}

			// Wasp nest
			EventHook.AddWithSync(waspNestFsm, "State 2");
		}

		public void SetPosAndRot(Vector3 pos, Quaternion rot) {
			Transform transform = gameObject.transform;
			transform.position = pos;
			transform.rotation = rot;
		}

		/// <summary>
		/// Set vehicle state
		/// </summary>
		public void SetEngineState(EngineStates state, DashboardStates dashstate, float startTime) {
			//Start time
			if (startTime != -1) {
				starterFsm.Fsm.GetFsmFloat("StartTime").Value = startTime;
			}

			// Engine states
			if (state == EngineStates.WaitForStart) {
				starterFsm.SendEvent("MP_Wait for start");
			}
			else if (state == EngineStates.ACC) {
				starterFsm.SendEvent("MP_ACC");
			}
			else if (state == EngineStates.TurnKey) {
				starterFsm.SendEvent("MP_Turn key");
			}
			else if (state == EngineStates.StartingEngine) {
				starterFsm.SendEvent("MP_Starting engine");
			}
			else if (state == EngineStates.StartEngine) {
				starterFsm.SendEvent("MP_Start engine");
			}
			else if (state == EngineStates.MotorRunning) {
				starterFsm.SendEvent("MP_Motor running");
			}
			else if (state == EngineStates.Wait) {
				starterFsm.SendEvent("MP_Wait");
			}
			else if (state == EngineStates.CheckClutch) {
				starterFsm.SendEvent("MP_Check clutch");
			}
			else if (state == EngineStates.StartOrNot) {
				starterFsm.SendEvent("MP_Start or not");
			}
			else if (state == EngineStates.Glowplug) {
				starterFsm.SendEvent("MP_ACC / Glowplug");
			}

			// Dashboard states
			if (dashstate == DashboardStates.ACCon) {
				dashboardFsm.SendEvent(MP_ACC_ON_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.Test) {
				dashboardFsm.SendEvent(MP_TEST_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.ACCon2) {
				dashboardFsm.SendEvent(MP_ACC_ON_2_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.MotorStarting) {
				dashboardFsm.SendEvent(MP_MOTOR_STARTING_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.ShutOff) {
				dashboardFsm.SendEvent(MP_SHUT_OFF_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.MotorOff) {
				dashboardFsm.SendEvent(MP_MOTOR_OFF_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.WaitButton) {
				dashboardFsm.SendEvent(MP_WAIT_BUTTON_EVENT_NAME);
			}
			else if (dashstate == DashboardStates.WaitPlayer) {
				dashboardFsm.SendEvent(MP_WAIT_PLAYER_EVENT_NAME);
			}
		}

		public void SetVehicleSwitch(SwitchIDs state, bool newValue, float newValueFloat) {
			Logger.Debug($"Remote vehicle switch {state.ToString()} set on vehicle: {VehicleTransform.gameObject.name} (New value: {newValue} New value float: {newValueFloat})");

			// Parking brake
			if (state == SwitchIDs.HandbrakePull) {
				handbrakeFsm.Fsm.GetFsmFloat("KnobPos").Value = newValueFloat;
			}

			// Truck parking brake
			else if (state == SwitchIDs.HandbrakeLever) {
				if (handbrakeFsm.Fsm.GetFsmBool("Brake").Value != newValue) {
					handbrakeFsm.SendEvent("MP_Flip");
				}
			}

			// Fuel tap
			else if (state == SwitchIDs.FuelTap) {
				if (fuelTapFsm.Fsm.GetFsmBool("FuelOn").Value != newValue) {
					fuelTapFsm.SendEvent("MP_Test");
				}
			}

			// Lights
			else if (state == SwitchIDs.Lights) {
				while (lightsFsm.Fsm.GetFsmInt("Selection").Value != newValueFloat) {
					lightsFsm.SendEvent("MP_Test");
				}
			}

			// Wipers
			else if (state == SwitchIDs.Wipers) {
				if (wipersFsm.Fsm.GetFsmInt("Selection").Value != newValueFloat) {
					wipersFsm.SendEvent("MP_Test 2");
				}
			}

			// Interior light
			else if (state == SwitchIDs.InteriorLight) {
				if (interiorLightFsm.Fsm.GetFsmBool("On").Value != newValue) {
					interiorLightFsm.SendEvent("MP_Switch");
				}
			}

			// Hydraulic pump
			else if (state == SwitchIDs.HydraulicPump) {
				if (hydraulicPumpFsm.Fsm.GetFsmBool("On").Value != newValue) {
					hydraulicPumpFsm.SendEvent("MP_Test");
				}
			}

			// Spill valve
			else if (state == SwitchIDs.SpillValve) {
				if (spillValveFsm.Fsm.GetFsmBool("Open").Value != newValue) {
					spillValveFsm.SendEvent("MP_Switch");
				}
			}

			// Axle lift
			else if (state == SwitchIDs.AxleLift) {
				if (axleLiftFsm.Fsm.GetFsmBool("Up").Value != newValue) {
					axleLiftFsm.SendEvent("MP_Test");
				}
			}

			// Diff lock
			else if (state == SwitchIDs.DiffLock) {
				if (diffLockFsm.Fsm.GetFsmBool("Lock").Value != newValue) {
					diffLockFsm.SendEvent("MP_Test");
				}
			}
		}

		public void UpdateIMGUI() {
			string vinfo = "Vehicle info:\n" +
				$"  Name: {gameObject.name}\n" +
				$"  Steering: {Steering}\n";

			if (starterFsm != null) {
				vinfo += "  > Starter\n";

				vinfo += $"     Active state: {starterFsm.Fsm.ActiveStateName}\n";
				if (starterFsm.Fsm.PreviousActiveState != null) {
					vinfo += $"     Prev Active state:  {starterFsm.Fsm.PreviousActiveState.Name}\n";
				}
				vinfo += $"     Start time: {starterFsm.Fsm.GetFsmFloat("StartTime").Value}\n";
			}

			if (dashboardFsm != null) {
				vinfo += "  > Dashboard:\n";
				vinfo += $"     Active state: {dashboardFsm.Fsm.ActiveStateName}\n";
				vinfo += $"     Prev Active state: {dashboardFsm.Fsm.PreviousActiveState.Name}\n";
			}

			if (lightsFsm != null) {
				vinfo += "  > Lights:\n";
				vinfo += $"     Active state: {lightsFsm.Fsm.ActiveStateName}\n";
				vinfo += $"     Prev Active state: {lightsFsm.Fsm.PreviousActiveState.Name}\n";
			}

			GUI.Label(new Rect(10, 200, 500, 500), vinfo);
		}
	}
}
