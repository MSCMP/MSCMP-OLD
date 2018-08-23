using System;
using UnityEngine;

namespace MSCMP.Network {

	/// <summary>
	/// Class representing network player.
	/// </summary>
	class NetPlayer : IDisposable {

		private Steamworks.CSteamID steamId = Steamworks.CSteamID.Nil;

		/// <summary>
		/// Offset for the character model.
		/// </summary>
		private Vector3 CHARACTER_OFFSET = new Vector3(0.0f, 0.60f, 0.0f);

		/// <summary>
		/// Steam id of the player.
		/// </summary>
		public Steamworks.CSteamID SteamId {
			get { return steamId; }
		}

		/// <summary>
		/// The network manager managing connection with this player.
		/// </summary>
		protected NetManager netManager = null;

		/// <summary>
		/// The anim manager managing connection with this player.
		/// </summary>
		protected PlayerAnimManager animManager = null;

		/// <summary>
		/// The game object representing character.
		/// </summary>
		private GameObject characterGameObject = null;

		/// <summary>
		/// Did this player handshake with us during this session?
		/// </summary>
		public bool hasHandshake = false;

		/// <summary>
		/// Character interpolator.
		/// </summary>
		Math.TransformInterpolator interpolator = new Math.TransformInterpolator();

		/// <summary>
		/// Picked up object interpolator.
		/// </summary>
		Math.TransformInterpolator pickedUpObjectInterpolator = new Math.TransformInterpolator();

		/// <summary>
		/// Interpolation time in miliseconds.
		/// </summary>
		public const ulong INTERPOLATION_TIME = NetLocalPlayer.SYNC_INTERVAL;

		/// <summary>
		/// Network time when sync packet was received.
		/// </summary>
		private ulong syncReceiveTime = 0;

		/// <summary>
		/// Name of the game object we use as prefab for characters.
		/// </summary>
		private const string CHARACTER_PREFAB_NAME = "Assets/MPPlayerModel/MPPlayerModel.fbx";

		/// <summary>
		/// Current player state.
		/// </summary>
		protected enum State {
			OnFoot,
			DrivingVehicle,
			Passenger
		}

		/// <summary>
		/// State of the player.
		/// </summary>
		protected State state = State.OnFoot;

		/// <summary>
		/// The current vehicle player is inside.
		/// </summary>
		protected NetVehicle currentVehicle = null;

		/// <summary>
		/// Network world this player is spawned in.
		/// </summary>
		protected NetWorld netWorld = null;

		/// <summary>
		/// The object the player has picked up.
		/// </summary>
		private GameObject pickedUpObject = null;

		/// <summary>
		/// The network id of object the player has picked up.
		/// </summary>
		private ushort pickedUpObjectNetId = NetPickupable.INVALID_ID;

		/// <summary>
		/// The old layer of the pickupable. Used to restore layer after releasing object.
		/// </summary>
		private int oldPickupableLayer = 0;

		/// <summary>
		/// Is this player spawned?
		/// </summary>
		/// <remarks>
		/// This state is valid only for remote players.
		/// </remarks>
		public bool IsSpawned {
			get { return characterGameObject != null; }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">Network manager managing connection to the player.</param>
		/// <param name="netWorld">Network world owning this player.</param>
		/// <param name="steamId">Player's steam id.</param>
		public NetPlayer(NetManager netManager, NetWorld netWorld, Steamworks.CSteamID steamId) {
			this.netManager = netManager;
			this.netWorld = netWorld;
			this.steamId = steamId;
		}

		/// <summary>
		/// Spawns character object in world.
		/// </summary>
		public void Spawn() {
			GameObject loadedModel = Client.LoadAsset<GameObject>(CHARACTER_PREFAB_NAME);
			characterGameObject = (GameObject)GameObject.Instantiate((GameObject)loadedModel, interpolator.CurrentPosition, interpolator.CurrentRotation);

			// If character will disappear we uncomment this
			// GameObject.DontDestroyOnLoad(go);

			//Getting the Animation component of the model, and setting the priority layers of each animation
			if (animManager == null) { animManager = new PlayerAnimManager(); Logger.Debug("AnimManager: JUST CREATED MORE!"); }
			else Logger.Debug("AnimManager: We had already (from NetLocal) our animManager");
			animManager.SetupAnimations(characterGameObject);

			/*if (characterAnimationComponent != null) {
				// Force character to stand.

				PlayAnimation(AnimationId.Standing, true);
			}*/

			if (pickedUpObjectNetId != NetPickupable.INVALID_ID) {
				UpdatePickedUpObject(true, false);
			}

			if (currentVehicle != null) {
				SitInCurrentVehicle();
			}
		}

		/// <summary>
		/// Cleanup all objects before destroying the player.
		/// </summary>
		public void Dispose() {

			if (currentVehicle != null) {
				LeaveVehicle();
			}

			// Destroy player model on disconnect/timeout.

			if (characterGameObject != null) {
				GameObject.Destroy(characterGameObject);
				characterGameObject = null;
			}
		}

		/// <summary>
		/// Send a packet to this player.
		/// </summary>
		/// <param name="data">The data to send.</param>
		/// <param name="sendType">Type of the send.</param>
		/// <param name="channel">The channel to send message.</param>
		/// <returns>true if packet was sent, false otherwise</returns>
		public bool SendPacket(byte[] data, Steamworks.EP2PSend sendType, int channel = 0) {
			return Steamworks.SteamNetworking.SendP2PPacket(this.steamId, data, (uint)data.Length, sendType, channel);
		}

		/// <summary>
		/// Updates state of the player.
		/// </summary>
		public virtual void Update() {

			// Some naive interpolation.

			if (characterGameObject && syncReceiveTime > 0) {
				float progress = (float)(netManager.GetNetworkClock() - syncReceiveTime) / INTERPOLATION_TIME;

				float speed = 0.0f;
				if (progress <= 2.0f) {
					Vector3 oldPos = interpolator.CurrentPosition;
					Vector3 currentPos = Vector3.zero;
					Quaternion currentRot = Quaternion.identity;
					interpolator.Evaluate(ref currentPos, ref currentRot, progress);
					Vector3 delta = (currentPos - oldPos);
					delta.y = 0.0f;
					speed = delta.magnitude;

					UpdateCharacterPosition();

					pickedUpObjectInterpolator.Evaluate(progress);
					UpdatePickedupPosition();
				}

				if (animManager != null) {
					animManager.HandleOnFootMovementAnimations(speed);
					animManager.CheckBlendedOutAnimationStates();
					animManager.SyncVerticalHeadLook(characterGameObject, progress);
				}
			}

		}

		/// <summary>
		/// Draw this player name tag.
		/// </summary>
		public void DrawNametag() {
			if (characterGameObject != null) {
				Vector3 spos = Camera.main.WorldToScreenPoint(characterGameObject.transform.position + Vector3.up * 2.0f);
				if (spos.z > 0.0f) {
					float width = 100.0f;
					spos.x -= width / 2.0f;
					GUI.color = Color.black;
					GUI.Label(new Rect(spos.x + 1, Screen.height - spos.y + 1, width, 20), GetName());
					GUI.color = Color.cyan;
					GUI.Label(new Rect(spos.x, Screen.height - spos.y, width, 20), GetName());
					GUI.color = Color.white;
				}
			}
		}

		/// <summary>
		/// Handle received synchronization message.
		/// </summary>
		/// <param name="msg">The received synchronization message.</param>
		public void HandleSynchronize(Messages.PlayerSyncMessage msg) {
			Client.Assert(state == State.OnFoot, "Received on foot update but player is not on foot.");

			Vector3 targetPos = Utils.NetVec3ToGame(msg.position);
			Quaternion targetRot = Utils.NetQuatToGame(msg.rotation);

			interpolator.SetTarget(targetPos, targetRot);
			syncReceiveTime = netManager.GetNetworkClock();

			if (msg.HasPickedUpData) {
				var pickedUpData = msg.PickedUpData;
				pickedUpObjectInterpolator.SetTarget(Utils.NetVec3ToGame(pickedUpData.position), Utils.NetQuatToGame(pickedUpData.rotation));
			}

			if (!IsSpawned) {
				Teleport(targetPos, targetRot);
				return;
			}
		}

		/// <summary>
		/// Handle received animation synchronization message.
		/// </summary>
		/// <param name="msg">The received synchronization message.</param>
		public void HandleAnimSynchronize(Messages.AnimSyncMessage msg) {
			if (animManager != null) animManager.HandleAnimations(msg);
		}

		/// <summary>
		/// Handle received vehicle synchronization message.
		/// </summary>
		/// <param name="msg">The received synchronization message.</param>
		public void HandleVehicleSync(Messages.VehicleSyncMessage msg) {
			if (!IsSpawned) {
				return;
			}

			Client.Assert(state == State.DrivingVehicle, "Received driving vehicle update but player is not driving any vehicle.");
			currentVehicle.HandleSynchronization(msg);
		}

		/// <summary>
		/// Sit in current vehicle.
		/// </summary>
		private void SitInCurrentVehicle() {
			if (currentVehicle != null) {

				// Make sure player character is attached as we will not update it's position until he leaves vehicle.

				if (IsSpawned) {
					Game.Objects.GameVehicle vehicleGameObject = currentVehicle.GameObject;
					if (state == State.DrivingVehicle) {
						Transform seatTransform = vehicleGameObject.SeatTransform;
						Teleport(seatTransform.position, seatTransform.rotation);
					}
					else if (state == State.Passenger) {
						Transform passangerSeatTransform = vehicleGameObject.PassengerSeatTransform;
						Teleport(passangerSeatTransform.position, passangerSeatTransform.rotation);
					}

					characterGameObject.transform.SetParent(vehicleGameObject.VehicleTransform, false);
				}
			}
		}

		/// <summary>
		/// Enter vehicle.
		/// </summary>
		/// <param name="vehicle">The vehicle to enter.</param>
		/// <param name="passenger">Is player entering vehicle as passenger?</param>
		public virtual void EnterVehicle(NetVehicle vehicle, bool passenger) {
			Client.Assert(currentVehicle == null, "Entered vehicle but player is already in vehicle.");
			Client.Assert(state == State.OnFoot, "Entered vehicle but player is not on foot.");

			// Set vehicle and put player inside it.

			currentVehicle = vehicle;
			currentVehicle.SetPlayer(this, passenger);

			SitInCurrentVehicle();

			// Set state of the player.

			SwitchState(passenger ? State.Passenger : State.DrivingVehicle);
		}

		/// <summary>
		/// Leave vehicle player is currently sitting in.
		/// </summary>
		public virtual void LeaveVehicle() {
			Client.Assert(currentVehicle != null && state != State.OnFoot, "Player is leaving vehicle but he is not in vehicle.");

			// Detach character game object from vehicle.

			if (IsSpawned) {
				characterGameObject.transform.SetParent(null);

				Game.Objects.GameVehicle vehicleGameObject = currentVehicle.GameObject;
				Transform seatTransform = vehicleGameObject.SeatTransform;
				Teleport(seatTransform.position, seatTransform.rotation);
			}

			// Notify vehicle that the player left.

			currentVehicle.ClearPlayer(state == State.Passenger);
			currentVehicle = null;

			// Set state of the player.

			SwitchState(State.OnFoot);
		}

		/// <summary>
		/// Switches state of this player.
		/// </summary>
		/// <param name="newState">The state to switch to.</param>
		protected virtual void SwitchState(State newState) {
			state = newState;
		}

		/// <summary>
		/// Teleport player to the given location.
		/// </summary>
		/// <param name="pos">The position to teleport to.</param>
		/// <param name="rot">The rotation to teleport to.</param>
		public virtual void Teleport(Vector3 pos, Quaternion rot) {
			interpolator.Teleport(pos, rot);
			UpdateCharacterPosition();
		}

		/// <summary>
		/// Update character position from interpolator.
		/// </summary>
		private void UpdateCharacterPosition() {
			if (characterGameObject != null) {
				characterGameObject.transform.position = interpolator.CurrentPosition + CHARACTER_OFFSET;
				characterGameObject.transform.rotation = interpolator.CurrentRotation;
			}
		}

		/// <summary>
		/// Update position of the picked up object.
		/// </summary>
		private void UpdatePickedupPosition() {
			if (pickedUpObject == null) {
				return;
			}

			pickedUpObject.transform.position = pickedUpObjectInterpolator.CurrentPosition;
			pickedUpObject.transform.rotation = pickedUpObjectInterpolator.CurrentRotation;
		}

		/// <summary>
		/// Get world position of the character.
		/// </summary>
		/// <returns>World position of the player character.</returns>
		public virtual Vector3 GetPosition() {
			return interpolator.CurrentPosition;
		}

		/// <summary>
		/// Get world rotation of the character.
		/// </summary>
		/// <returns>World rotation of the player character.</returns>
		public virtual Quaternion GetRotation() {
			return interpolator.CurrentRotation;
		}

		/// <summary>
		/// Get steam name of the player.
		/// </summary>
		/// <returns>Steam name of the player.</returns>
		public virtual string GetName() {
			return Steamworks.SteamFriends.GetFriendPersonaName(steamId);
		}

		/// <summary>
		/// Pickup the object.
		/// </summary>
		/// <param name="netId">netId of the object to pickup</param>
		public void PickupObject(ushort netId) {
			pickedUpObjectNetId = netId;

			// Teleport picked up object position to perform much nicer transition
			// of object interpolation. Previously the object was interpolated from last frame.

			pickedUpObjectInterpolator.Teleport(interpolator.CurrentPosition, interpolator.CurrentRotation);
			if (IsSpawned) {
				UpdatePickedUpObject(true, false);
			}
		}

		/// <summary>
		/// Release the object.
		/// </summary>
		/// <param name="drop">Is it drop or throw?</param>
		public void ReleaseObject(bool drop) {
			if (IsSpawned) {
				UpdatePickedUpObject(false, drop);
			}
			pickedUpObjectNetId = NetPickupable.INVALID_ID;
		}

		/// <summary>
		/// Update picked up object.
		/// </summary>
		/// <param name="pickup">Is this pickup action?</param>
		/// <param name="drop">If not pickup is it drop or throw?</param>
		private void UpdatePickedUpObject(bool pickup, bool drop) {
			if (pickup) {
				pickedUpObject = netWorld.GetPickupableGameObject(pickedUpObjectNetId);
				Client.Assert(pickedUpObject != null, "Player tried to pickup object that does not exists in world. Net id: " + pickedUpObjectNetId);
				oldPickupableLayer = pickedUpObject.layer;
				pickedUpObject.layer = Utils.LAYER_IGNORE_RAYCAST;
				pickedUpObject.GetComponent<Rigidbody>().isKinematic = true;
			}
			else {
				Client.Assert(pickedUpObject != null, "Tried to drop item however player has no item in hands.");
				pickedUpObject.layer = oldPickupableLayer;
				pickedUpObject.GetComponent<Rigidbody>().isKinematic = false;
				if (!drop) {
					float thrust = 50;
					pickedUpObject.GetComponent<Rigidbody>().AddForce(pickedUpObject.transform.forward * thrust, ForceMode.Impulse);
				}
				pickedUpObject = null;
			}
		}
	}
}
