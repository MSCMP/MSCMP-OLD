using MSCMP.Game.Objects;
using MSCMP.Game;
using MSCMP.Math;
using UnityEngine;

namespace MSCMP.Network {
	class NetVehicle {

		/// <summary>
		/// Invalid id of the vehicle.
		/// </summary>
		public const byte INVALID_ID = NetWorld.MAX_VEHICLES;

		/// <summary>
		/// The synchronization delay when sending vehicle data.
		/// </summary>
		public const ulong SYNC_DELAY = 100;

		/// <summary>
		/// Network id of the vehicle.
		/// </summary>
		byte networkId;

		/// <summary>
		/// Name of the game object.
		/// </summary>
		string gameObjectName;

		/// <summary>
		/// Game object representing vehicle.
		/// </summary>
		public GameVehicle GameObject {
			get {
				return GameWorld.Instance.FindVehicleByName(gameObjectName);
			}
		}

		/// <summary>
		/// Get network id of this object.
		/// </summary>
		public byte NetId {
			get {
				return networkId;
			}
		}

		/// <summary>
		/// Vehicle transform interpolator.
		/// </summary>
		TransformInterpolator interpolator = new TransformInterpolator();

		/// <summary>
		/// The network time when sync was received.
		/// </summary>
		ulong syncReceiveTime = 0;

		/// <summary>
		/// The network manager owning this object.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// The driver player.
		/// </summary>
		NetPlayer driverPlayer = null;

		/// <summary>
		/// The passenger player.
		/// </summary>
		NetPlayer passengerPlayer = null;

		/// <summary>
		/// Last engine state.
		/// </summary>
		GameVehicle.EngineStates lastEngineState;

		/// <summary>
		/// Latest received value.
		/// </summary>
		int lastGear = 0;
		bool lastRange = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameName">Name of the game object representing this vehicle.</param>
		public NetVehicle(NetManager netManager, string gameName, byte netId) {
			this.netManager = netManager;
			this.networkId = netId;
			this.gameObjectName = gameName;
		}

		/// <summary>
		/// Update state of the network vehicle. (can be called more than once per frame!)
		/// </summary>
		public virtual void FixedUpdate() {
			// If local player is driving this vehicle do not interpolate it's position.

			if (driverPlayer is NetLocalPlayer) {
				return;
			}

			// Perform interpolation.

			if (GameObject != null && syncReceiveTime > 0) {
				float alpha = (float)(netManager.GetNetworkClock() - syncReceiveTime) / SYNC_DELAY;
				if (alpha > 2.0f) {
					return;
				}

				UpdateTransform(alpha);
			}
		}

		/// <summary>
		/// Update state of the vehicle each frame.
		/// </summary>
		public virtual void Update() {
			ApplyRemoteSteering();
		}

		/// <summary>
		/// Apply vehicles remote steering.
		/// </summary>
		private void ApplyRemoteSteering() {
			if (GameObject == null) {
				return;
			}

			bool remoteSteering = (driverPlayer != null) && !(driverPlayer is NetLocalPlayer);
			if (GameObject.RemoteSteering != remoteSteering) {
				GameObject.RemoteSteering = remoteSteering;
			}
		}

		/// <summary>
		/// Update transform of vehicle from interpolator.
		/// </summary>
		/// <param name="alpha">The interpolator alpha.</param>
		public void UpdateTransform(float alpha = 0.0f) {
			Vector3 pos = Vector3.zero;
			Quaternion rot = Quaternion.identity;
			interpolator.Evaluate(ref pos, ref rot, alpha);
			GameObject.SetPosAndRot(pos, rot);
		}


		/// <summary>
		/// Handle synchronization packet from the network.
		/// </summary>
		/// <param name="msg">The message to handle.</param>
		public virtual void HandleSynchronization(Messages.VehicleSyncMessage message) {
			interpolator.SetTarget(Utils.NetVec3ToGame(message.position), Utils.NetQuatToGame(message.rotation));
			syncReceiveTime = netManager.GetNetworkClock();

			if (GameObject != null) {
				GameObject.Steering = message.steering;
				GameObject.Throttle = message.throttle;
				GameObject.Brake = message.brake;
				GameObject.ClutchInput = message.clutch;
				GameObject.Fuel = message.fuel;
				if (message.HasGear) {
					GameObject.Gear = message.Gear;
				}
				if (message.HasRange) {
					GameObject.Range = message.Range;
				}
				if (message.HasHydraulic) {
					GameObject.FrontHydraulic = message.Hydraulic;
				}
			}
		}

		/// <summary>
		/// Set player inside vehicle.
		/// </summary>
		/// <param name="player">The player to set.</param>
		/// <param name="passenger">Whether to set player as passenger or as driver.</param>
		public void SetPlayer(NetPlayer player, bool passenger) {
			if (passenger) {
				passengerPlayer = player;
			}
			else {
				driverPlayer = player;

				bool remoteSteering = (driverPlayer != null) && !(driverPlayer is NetLocalPlayer);
				GameObject.SetRemoteSteering(remoteSteering);
			}
		}

		/// <summary>
		/// Clear player at the given slot.
		/// </summary>
		/// <param name="passenger">Whether to clear passenger or driver?</param>
		public void ClearPlayer(bool passenger) {
			SetPlayer(null, passenger);
		}

		/// <summary>
		/// Callback called when game world gets loaded.
		/// </summary>
		public void OnGameWorldLoad() {
			GameObject.onEnter = (bool passenger) => {
				netManager.GetLocalPlayer().EnterVehicle(this, passenger);
			};

			GameObject.onLeave = () => {
				netManager.GetLocalPlayer().LeaveVehicle();
			};

			GameObject.onEngineStateChanged = (GameVehicle.EngineStates state, GameVehicle.DashboardStates dashstate, float startTime) => {
				if (lastEngineState != state) {
					netManager.GetLocalPlayer().WriteVehicleStateMessage(this, state, dashstate, startTime);
					lastEngineState = state;
				}
			};

			GameObject.onVehicleSwitchChanges = (GameVehicle.SwitchIDs id, bool newValue, float newValueFloat) => {
				netManager.GetLocalPlayer().WriteVehicleSwitchMessage(this, id, newValue, newValueFloat);
			};


			// Make sure interpolator has proper location of the vehicle.

			interpolator.Teleport(GameObject.VehicleTransform.position, GameObject.VehicleTransform.rotation);
		}

		/// <summary>
		/// Write vehicle data into sync message.
		/// </summary>
		/// <param name="message">The sync message to write to.</param>
		public bool WriteSyncMessage(Messages.VehicleSyncMessage message) {
			if (GameObject == null) {
				return false;
			}

			Transform transform = GameObject.VehicleTransform;
			if (!transform) {
				return false;
			}
			message.position = Utils.GameVec3ToNet(transform.position);
			message.rotation = Utils.GameQuatToNet(transform.rotation);
			message.steering = GameObject.Steering;
			message.throttle = GameObject.Throttle;
			message.brake = GameObject.Brake;
			message.clutch = GameObject.ClutchInput;
			message.fuel = GameObject.Fuel;

			//Only send following messages when they have changed
			if (GameObject.Gear != lastGear) {
				lastGear = GameObject.Gear;
				message.Gear = GameObject.Gear;
			}
			if (GameObject.hasRange == true && GameObject.Range != lastRange) {
				lastRange = GameObject.Range;
				message.Range = GameObject.Range;
			}
			if (GameObject.isTractor == true) {
				message.Hydraulic = GameObject.FrontHydraulic;
			}
			return true;
		}

		/// <summary>
		/// Sets engine and dashboard state of vehicle.
		/// </summary>
		/// <param name="state">Engine state to change.</param>
		/// <param name="dashstate">Dashboard state to change.</param>
		/// <param name="startTime">Engine start time.</param>
		public void SetEngineState(int state, int dashstate, float startTime) {
			GameVehicle.EngineStates engineState = (GameVehicle.EngineStates)state;
			GameVehicle.DashboardStates dashState = (GameVehicle.DashboardStates)dashstate;
			GameObject.SetEngineState(engineState, dashState, startTime);
		}

		public void SetVehicleSwitch(int id, bool newValue, float newValueFloat) {
			GameVehicle.SwitchIDs switchID = (GameVehicle.SwitchIDs)id;
			GameObject.SetVehicleSwitch(switchID, newValue, newValueFloat);
		}

		/// <summary>
		/// Teleport vehicle to the given placement.
		/// </summary>
		/// <param name="position">The position to teleport vehicle to.</param>
		/// <param name="rotaton">The rotaton to teleport vehicle to.</param>
		public void Teleport(Vector3 position, Quaternion rotation) {
			interpolator.Teleport(position, rotation);
			UpdateTransform();
		}

		/// <summary>
		/// Get vehicles position.
		/// </summary>
		/// <returns>Vehicle position.</returns>
		public Vector3 GetPosition() {
			if (GameObject == null) {
				return interpolator.CurrentPosition;
			}
			return GameObject.VehicleTransform.position;
		}

		/// <summary>
		/// Get vehicles rotation.
		/// </summary>
		/// <returns>Vehicle rotation.</returns>
		public Quaternion GetRotation() {
			if (GameObject == null) {
				return interpolator.CurrentRotation;
			}
			return GameObject.VehicleTransform.rotation;
		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Draw debug ui about this vehicle.
		/// </summary>
		public void UpdateIMGUI() {
			if (GameObject != null) {
				GameObject.UpdateIMGUI();
			}
		}
#endif
	}
}
