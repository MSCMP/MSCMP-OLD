using MSCMP.Game;
using MSCMP.Game.Objects;
using UnityEngine;

namespace MSCMP.Network {

	/// <summary>
	/// Class handling local player state.
	/// </summary>
	class NetLocalPlayer : NetPlayer {

		/// <summary>
		/// Instance.
		/// </summary>
		public static NetLocalPlayer Instance = null;

		/// <summary>
		/// How much time in seconds left until next synchronization packet will be sent.
		/// </summary>
		private float timeToUpdate = 0.0f;

		/// <summary>
		/// Synchronization interval in milliseconds.
		/// </summary>
		public const ulong SYNC_INTERVAL = 100;

		/// <summary>
		/// SteamID of local player.
		/// </summary>
		private Steamworks.CSteamID steamID;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">The network manager owning this player.</param>
		/// <param name="netWorld">Network world owning this player.</param>
		/// <param name="steamId">The steam id of the player.</param>
		public NetLocalPlayer(NetManager netManager, NetWorld netWorld, Steamworks.CSteamID steamId) : base(netManager, netWorld, steamId) {
			Instance = this;
			steamID = steamId;

			GameDoorsManager.Instance.onDoorsOpen = (GameObject door) => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.position = Utils.GameVec3ToNet(door.transform.position);
				msg.open = true;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};

			GameDoorsManager.Instance.onDoorsClose = (GameObject door) => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.position = Utils.GameVec3ToNet(door.transform.position);
				msg.open = false;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};

			LightSwitchManager.Instance.onLightSwitchUsed = (GameObject lswitch, bool turnedOn) => {
				Messages.LightSwitchMessage msg = new Messages.LightSwitchMessage();
				msg.pos = Utils.GameVec3ToNet(lswitch.transform.position);
				msg.toggle = turnedOn;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};

			if (animManager == null) animManager = new PlayerAnimManager();
		}

		/// <summary>
		/// Update state of the local player.
		/// </summary>
		public override void Update() {
			if (state == State.Passenger) {
				// Skip update when we don't have anything to do.
				return;
			}

			// Synchronization sending.

			timeToUpdate -= Time.deltaTime;
			if (timeToUpdate <= 0.0f && netManager.IsPlaying) {
				if (animManager != null) {
					animManager.PACKETS_LEFT_TO_SYNC--;
					if (animManager.PACKETS_LEFT_TO_SYNC <= 0) {
						animManager.PACKETS_LEFT_TO_SYNC = animManager.PACKETS_TOTAL_FOR_SYNC;
						SendAnimSync();
					}
				}

				switch (state) {
					case State.OnFoot:
						SendOnFootSync();
						break;
				}
			}
		}

		/// <summary>
		/// Send on foot sync to the server.
		/// </summary>
		/// <returns>true if sync message was sent false otherwise</returns>
		private bool SendOnFootSync() {
			GamePlayer player = GameWorld.Instance.Player;
			if (player == null) {
				return false;
			}
			GameObject playerObject = player.Object;
			if (playerObject == null) {
				return false;
			}

			Messages.PlayerSyncMessage message = new Messages.PlayerSyncMessage();

			message.position = Utils.GameVec3ToNet(playerObject.transform.position);
			message.rotation = Utils.GameQuatToNet(playerObject.transform.rotation);

			if (player.PickedUpObject) {
				Transform objectTrans = player.PickedUpObject.transform;
				var data = new Messages.PickedUpSync();
				data.position = Utils.GameVec3ToNet(objectTrans.position);
				data.rotation = Utils.GameQuatToNet(objectTrans.rotation);

				message.PickedUpData = data;
			}

			if (!netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendUnreliable)) {
				return false;
			}

			timeToUpdate = (float)SYNC_INTERVAL / 1000;
			return true;
		}

		/// <summary>
		/// Send anim sync to the server.
		/// </summary>
		/// <returns>true if sync message was sent false otherwise</returns>
		private bool SendAnimSync() {
			GamePlayer player = GameWorld.Instance.Player;
			if (player == null) return false;

			GameObject playerObject = player.Object;
			if (playerObject == null) return false;

			if (playerObject.GetComponentInChildren<CharacterMotor>() == null) return false; //Player is dying!

			Messages.AnimSyncMessage message = new Messages.AnimSyncMessage();

			message.isRunning = (Utils.GetPlaymakerScriptByName(playerObject, "Running").Fsm.ActiveStateName == "Run");

			float leanRotation = Utils.GetPlaymakerScriptByName(playerObject, "Reach").Fsm.GetFsmFloat("Position").Value;
			if (leanRotation != 0.0f) message.isLeaning = true;
			else message.isLeaning = false;

			message.isGrounded = playerObject.GetComponentInChildren<CharacterMotor>().grounded;

			message.activeHandState = animManager.GetActiveHandState(playerObject);

			message.swearId = int.MaxValue;
			if (animManager.GetHandState(message.activeHandState) == PlayerAnimManager.HandStateId.MiddleFingering) {
				message.swearId = Utils.GetPlaymakerScriptByName(playerObject, "PlayerFunctions").Fsm.GetFsmInt("RandomInt").Value;
			}
			PlayMakerFSM speechFsm = Utils.GetPlaymakerScriptByName(playerObject, "Speech");
			if (speechFsm.ActiveStateName == "Swear") message.swearId = animManager.Swears_Offset + speechFsm.Fsm.GetFsmInt("RandomInt").Value;
			else if (speechFsm.ActiveStateName == "Drunk speech") message.swearId = animManager.DrunkSpeaking_Offset + speechFsm.Fsm.GetFsmInt("RandomInt").Value;
			else if (speechFsm.ActiveStateName == "Yes gestures") message.swearId = animManager.Agreeing_Offset + speechFsm.Fsm.GetFsmInt("RandomInt").Value;

			message.aimRot = playerObject.transform.FindChild("Pivot/Camera/FPSCamera").transform.rotation.eulerAngles.x;
			message.crouchPosition = Utils.GetPlaymakerScriptByName(playerObject, "Crouch").Fsm.GetFsmFloat("Position").Value;

			GameObject DrunkObject = playerObject.transform.FindChild("Pivot/Camera/FPSCamera/FPSCamera").gameObject;
			float DrunkValue = Utils.GetPlaymakerScriptByName(DrunkObject, "Drunk Mode").Fsm.GetFsmFloat("DrunkYmax").Value;
			if (DrunkValue >= 4.5f) message.isDrunk = true;
			else message.isDrunk = false;

			if (!animManager.AreDrinksPreloaded()) animManager.PreloadDrinkObjects(playerObject);
			message.drinkId = animManager.GetDrinkingObject(playerObject);

			if (!netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendUnreliable)) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Enter vehicle.
		/// </summary>
		/// <param name="vehicle">The vehicle to enter.</param>
		/// <param name="passenger">Is player entering vehicle as passenger?</param>
		public override void EnterVehicle(Game.Components.ObjectSyncComponent vehicle, bool passenger) {
			base.EnterVehicle(vehicle, passenger);

			Messages.VehicleEnterMessage msg = new Messages.VehicleEnterMessage();
			msg.objectID = vehicle.ObjectID;
			msg.passenger = passenger;
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			vehicle.TakeSyncControl();
		}

		/// <summary>
		/// Leave vehicle player is currently sitting in.
		/// </summary>
		public override void LeaveVehicle() {
			base.LeaveVehicle();

			netManager.BroadcastMessage(new Messages.VehicleLeaveMessage(), Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Write vehicle engine state into state message.
		/// </summary>
		/// <param name="state">The engine state to write.</param>
		public void WriteVehicleStateMessage(Game.Components.ObjectSyncComponent vehicle, PlayerVehicle.EngineStates state, PlayerVehicle.DashboardStates dashstate, float startTime) {
			Messages.VehicleStateMessage msg = new Messages.VehicleStateMessage();
			msg.objectID = vehicle.ObjectID;
			msg.state = (int)state;
			msg.dashstate = (int)dashstate;
			if (startTime != -1) {
				msg.StartTime = startTime;
			}
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Write vehicle switch changes into vehicle switch message.
		/// </summary>
		/// <param name="state">The engine state to write.</param>
		public void WriteVehicleSwitchMessage(Game.Components.ObjectSyncComponent vehicle, PlayerVehicle.SwitchIDs switchID, bool newValue, float newValueFloat) {
			Messages.VehicleSwitchMessage msg = new Messages.VehicleSwitchMessage();
			msg.objectID = vehicle.ObjectID;
			msg.switchID = (int)switchID;
			msg.switchValue = newValue;
			if (newValueFloat != -1) {
				msg.SwitchValueFloat = newValueFloat;
			}
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Write player state into the network message.
		/// </summary>
		/// <param name="msg">Message to write to.</param>
		public void WriteSpawnState(Messages.FullWorldSyncMessage msg) {
			msg.spawnPosition = Utils.GameVec3ToNet(GetPosition());
			msg.spawnRotation = Utils.GameQuatToNet(GetRotation());

			msg.pickedUpObject = NetPickupable.INVALID_ID;
		}

		/// <summary>
		/// Send object sync.
		/// </summary>
		/// <param name="objectID">The Object ID of the object.</param>
		/// <param name="setOwner">Set owner of the object.</param>
		public void SendObjectSync(int objectID, Vector3 pos, Quaternion rot, ObjectSyncManager.SyncTypes syncType, float[] syncedVariables) {
			Messages.ObjectSyncMessage msg = new Messages.ObjectSyncMessage();
			msg.objectID = objectID;
			msg.position = Utils.GameVec3ToNet(pos);
			msg.rotation = Utils.GameQuatToNet(rot);
			msg.SyncType = (int)syncType;
			if (syncedVariables != null) {
				msg.SyncedVariables = syncedVariables;
			}
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Request object sync from the host.
		/// </summary>
		/// <param name="objectID">The Object ID of the object.</param>
		public void RequestObjectSync(int objectID) {
			Messages.ObjectSyncRequestMessage msg = new Messages.ObjectSyncRequestMessage();
			msg.objectID = objectID;
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Send object sync.
		/// </summary>
		/// <param name="objectID">The Object ID of the object.</param>
		/// <param name="accepted">If request to take sync ownership was accepted.</param>
		public void SendObjectSyncResponse(int objectID, bool accepted) {
			Messages.ObjectSyncResponseMessage msg = new Messages.ObjectSyncResponseMessage();
			msg.objectID = objectID;
			msg.accepted = accepted;
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}
		
		/// <summary>
		/// Send EventHook sync message.
		/// </summary>
		/// <param name="fsmID">FSM ID</param>
		/// <param name="fsmEventID">FSM Event ID</param>
		/// <param name="fsmEventName">Optional FSM Event name</param>
		public void SendEventHookSync(int fsmID, int fsmEventID, string fsmEventName = "none") {
			Messages.EventHookSyncMessage msg = new Messages.EventHookSyncMessage();
			msg.fsmID = fsmID;
			msg.fsmEventID = fsmEventID;
			msg.request = false;
			if (fsmEventName != "none") {
				msg.FsmEventName = fsmEventName;
			}
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Request event sync from host.
		/// </summary>
		/// <param name="fsmID">FSM ID</param>
		/// <param name="fsmEventID">FSM Event ID</param>
		public void RequestEventHookSync(int fsmID) {
			Messages.EventHookSyncMessage msg = new Messages.EventHookSyncMessage();
			msg.fsmID = fsmID;
			msg.fsmEventID = -1;
			msg.request = true;
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Switches state of this player.
		/// </summary>
		/// <param name="newState">The state to switch to.</param>
		protected override void SwitchState(State newState) {
			if (state == newState) {
				return;
			}

			base.SwitchState(newState);

			// Force synchronization to be send on next frame.
			timeToUpdate = 0.0f;
		}

		/// <summary>
		/// Get world position of the character.
		/// </summary>
		/// <returns>World position of the player character.</returns>
		public override Vector3 GetPosition() {
			GamePlayer player = GameWorld.Instance.Player;
			if (player == null) {
				return Vector3.zero;
			}
			var playerObject = player.Object;
			if (playerObject == null) {
				return Vector3.zero;
			}
			return playerObject.transform.position;
		}

		/// <summary>
		/// Get world rotation of the character.
		/// </summary>
		/// <returns>World rotation of the player character.</returns>
		public override Quaternion GetRotation() {
			GamePlayer player = GameWorld.Instance.Player;
			if (player == null) {
				return Quaternion.identity;
			}
			var playerObject = player.Object;
			if (playerObject == null) {
				return Quaternion.identity;
			}
			return playerObject.transform.rotation;
		}

		/// <summary>
		/// Get steam name of the player.
		/// </summary>
		/// <returns>Steam name of the player.</returns>
		public override string GetName() {
			return Steamworks.SteamFriends.GetPersonaName();
		}

		/// <summary>
		/// Teleport player to the given location.
		/// </summary>
		/// <param name="pos">The position to teleport to.</param>
		/// <param name="rot">The rotation to teleport to.</param>
		public override void Teleport(Vector3 pos, Quaternion rot) {
			GamePlayer player = GameWorld.Instance.Player;
			if (player == null) {
				return;
			}
			var playerObject = player.Object;
			if (playerObject == null) {
				return;
			}
			playerObject.transform.position = pos;
			playerObject.transform.rotation = rot;
		}
	}
}
