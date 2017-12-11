using MSCMP.Game;
using UnityEngine;

namespace MSCMP.Network {

	/// <summary>
	/// Class handling local player state.
	/// </summary>
	class NetLocalPlayer : NetPlayer {

		/// <summary>
		/// How much time in seconds left until next synchronization packet will be sent.
		/// </summary>
		private float timeToUpdate = 0.0f;

		/// <summary>
		/// Synchronization interval in milliseconds.
		/// </summary>
		public const ulong SYNC_INTERVAL = 100;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">The network manager owning this player.</param>
		/// <param name="steamId">The steam id of the player.</param>
		public NetLocalPlayer(NetManager netManager, Steamworks.CSteamID steamId) : base(netManager, steamId) {

			GameDoorsManager.Instance.onDoorsOpen = () => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.open = true;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};

			GameDoorsManager.Instance.onDoorsClose = () => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.open = false;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};
		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Update debug IMGUI for the player.
		/// </summary>
		public override void DrawDebugGUI() {
			GUI.Label(new Rect(300, 10, 300, 200), "Local player\ntime to update: " + timeToUpdate + "\nstate: " + state + "\nobject: " + GameWorld.Instance.PlayerGameObject);
		}
#endif

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

				switch (state) {
					case State.DrivingVehicle:
						SendInVehicleSync();
						break;

					case State.OnFoot:
						SendOnFootSync();
						break;
				}
			}
		}

		/// <summary>
		/// Send in vehicle synchronization.
		/// </summary>
		/// <returns>true if sync was set, false otherwise</returns>
		private bool SendInVehicleSync() {
			Client.Assert(currentVehicle != null, "Tried to send in vehicle sync packet but no current vehicle is set.");

			Messages.VehicleSyncMessage message = new Messages.VehicleSyncMessage();
			if (!currentVehicle.WriteSyncMessage(message)) {
				return false;
			}
			if (!netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendUnreliable)) {
				return false;
			}

			timeToUpdate = (float)NetVehicle.SYNC_DELAY / 1000;
			return true;
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

			Vector3 position = playerObject.transform.position;
			Quaternion rotation = playerObject.transform.rotation;

			Messages.PlayerSyncMessage message = new Messages.PlayerSyncMessage();
			message.position.x = position.x;
			message.position.y = position.y;
			message.position.z = position.z;

			message.rotation.w = rotation.w;
			message.rotation.x = rotation.x;
			message.rotation.y = rotation.y;
			message.rotation.z = rotation.z;
			if (!netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendUnreliable)) {
				return false;
			}

			timeToUpdate = (float)SYNC_INTERVAL / 1000;
			return true;
		}

		/// <summary>
		/// Enter vehicle.
		/// </summary>
		/// <param name="vehicle">The vehicle to enter.</param>
		/// <param name="passenger">Is player entering vehicle as passenger?</param>
		public override void EnterVehicle(NetVehicle vehicle, bool passenger) {
			base.EnterVehicle(vehicle, passenger);

			Messages.VehicleEnterMessage msg = new Messages.VehicleEnterMessage();
			msg.vehicleId = vehicle.NetId;
			msg.passenger = false;
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Leave vehicle player is currently sitting in.
		/// </summary>
		public override void LeaveVehicle() {
			base.LeaveVehicle();

			netManager.BroadcastMessage(new Messages.VehicleLeaveMessage(), Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Write player state into handshake message.
		/// </summary>
		/// <param name="msg">Message to write to.</param>
		public void WriteHandshake(Messages.HandshakeMessage msg) {
			if (state == State.OnFoot) {
				msg.occupiedVehicleId = NetVehicle.INVALID_ID;
			}
			else {
				msg.occupiedVehicleId = currentVehicle.NetId;
				msg.passenger = state == State.Passenger;
			}
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
		/// Get steam name of the player.
		/// </summary>
		/// <returns>Steam name of the player.</returns>
		public override string GetName() {
			return Steamworks.SteamFriends.GetPersonaName();
		}
	}
}
