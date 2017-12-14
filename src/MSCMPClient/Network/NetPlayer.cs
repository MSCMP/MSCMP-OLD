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
		private Vector3 CHARACTER_OFFSET = new Vector3(0.0f, -0.16f, 0.0f);

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
		/// The game object representing character.
		/// </summary>
		private GameObject characterGameObject = null;

		/// <summary>
		/// Cached animation component.
		/// </summary>
		private Animation characterAnimationComponent = null;

		/// <summary>
		/// The current animation state.
		/// </summary>
		private AnimationState activeAnimationState = null;

		/// <summary>
		/// The animation ids.
		/// </summary>
		enum AnimationId {
			Walk,
			Standing,
		}

		private string[] AnimationNames = new string[] {
			"fat_walk",
			"fat_standing"
		};

		/// <summary>
		/// Currently played animation id.
		/// </summary>
		private AnimationId currentAnim = AnimationId.Standing;

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
		private const string CHARACTER_PREFAB_NAME = "Hullu";

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
			GameObject prefab = GameObject.Find(CHARACTER_PREFAB_NAME);

			characterGameObject = (GameObject)GameObject.Instantiate((GameObject)prefab, interpolator.CurrentPosition, interpolator.CurrentRotation);

			// If character will disappear we uncomment this
			// GameObject.DontDestroyOnLoad(go);

			// Remove all FSM's we do not want to have for the character.

			Action<string> RemoveFSM = (string name) => {
				PlayMakerFSM fsm = Utils.GetPlaymakerScriptByName(characterGameObject, name);
				if (fsm != null) {
					GameObject.Destroy(fsm);

					Logger.Log("REMOVED " + name + " FSM!");
				}
			};

			RemoveFSM("Move"); // Performs random character moves.
			RemoveFSM("Obstacle"); // Rotates character to side to not walk "to the wall" when touching it.
			RemoveFSM("Raycast"); // Snaps character to ground.
			RemoveFSM("CarHit"); // Spawns ragdoll when character gets hit by car.

			characterAnimationComponent = characterGameObject.GetComponentInChildren<Animation>();
			if (characterAnimationComponent != null) {
				// Force character to stand.

				PlayAnimation(AnimationId.Standing, true);
			}

			if (pickedUpObjectNetId != NetPickupable.INVALID_ID) {
				UpdatePickedUpObject(true, false);
			}
		}

		/// <summary>
		/// Cleanup all objects before destroying the player.
		/// </summary>
		public void Dispose() {
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
		/// Convert animation id to it's name.
		/// </summary>
		/// <param name="animation">The id of the animation.</param>
		/// <returns>Name of the animation.</returns>
		private string GetAnimationName(AnimationId animation) {
			return AnimationNames[(int)animation];
		}

		/// <summary>
		/// Play selected animation.
		/// </summary>
		/// <param name="animation"></param>
		/// <param name="force"></param>
		private void PlayAnimation(AnimationId animation, bool force = false) {
			if (!force && currentAnim == animation) {
				return;
			}

			currentAnim = animation;
			if (characterAnimationComponent == null) {
				return;
			}

			string animName = GetAnimationName(animation);
			if (force) {
				characterAnimationComponent.Play(animName);
			}
			else {
				characterAnimationComponent.CrossFade(animName);
			}

			activeAnimationState = characterAnimationComponent[animName];
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

				if (speed > 0.001f) {
					PlayAnimation(AnimationId.Walk);

					// Set speed of the animation according to the speed of movement.

					activeAnimationState.speed = (speed * 60.0f) / activeAnimationState.length;
				}
				else {
					PlayAnimation(AnimationId.Standing);
				}
			}

		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Update debug IMGUI of this player.
		/// </summary>
		public virtual void DrawDebugGUI() {
			GUI.Label(new Rect(300, 200, 300, 200), "Remote player\n" +
				"position = "	+ interpolator.CurrentPosition.ToString() + "\n" +
				"anim = "		+ currentAnim + "\n" +
				"animSpeed = "	+ (activeAnimationState == null ? 0 : activeAnimationState.speed) + "\n" +
				"state = "		+ state + "\n"
			);

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
#endif

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

			if (!characterGameObject) {
				Teleport(targetPos, targetRot);
				return;
			}
		}

		/// <summary>
		/// Handle received vehicle synchronization message.
		/// </summary>
		/// <param name="msg">The received synchronization message.</param>
		public void HandleVehicleSync(Messages.VehicleSyncMessage msg) {
			Client.Assert(state == State.DrivingVehicle, "Received driving vehicle update but player is not driving any vehicle.");
			currentVehicle.HandleSynchronization(msg);
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

			// Make sure player character is attached as we will not update it's position until he leaves vehicle.

			if (characterGameObject != null) {
				characterGameObject.transform.SetParent(currentVehicle.GameObject.VehicleTransform, false);
			}

			// Set state of the player.

			SwitchState(passenger ? State.Passenger : State.DrivingVehicle);
		}

		/// <summary>
		/// Leave vehicle player is currently sitting in.
		/// </summary>
		public virtual void LeaveVehicle() {
			Client.Assert(currentVehicle != null && state != State.OnFoot, "Player is leaving vehicle but he is not in vehicle.");

			// Detach character game object from vehicle.

			if (characterGameObject != null) {
				characterGameObject.transform.SetParent(null);

				// TODO: Teleport interpolator to use current position so ped will not be interpolated from previous on foot location here.
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
		public void Teleport(Vector3 pos, Quaternion rot) {
			interpolator.Teleport(pos, rot);
			UpdateCharacterPosition();
		}

		/// <summary>
		/// Update character position from interpolator.
		/// </summary>
		private void UpdateCharacterPosition() {
			if (characterGameObject == null) {
				return;
			}

			characterGameObject.transform.position = interpolator.CurrentPosition + CHARACTER_OFFSET;
			characterGameObject.transform.rotation = interpolator.CurrentRotation;

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
			UpdatePickedUpObject(true, false);
		}

		/// <summary>
		/// Release the object.
		/// </summary>
		/// <param name="drop">Is it drop or throw?</param>
		public void ReleaseObject(bool drop) {
			UpdatePickedUpObject(false, drop);
			pickedUpObjectNetId = NetPickupable.INVALID_ID;
		}

		/// <summary>
		/// Update picked up object.
		/// </summary>
		/// <param name="pickup">Is this pickup action?</param>
		/// <param name="drop">If not pickup is it drop or throw?</param>
		private void UpdatePickedUpObject(bool pickup, bool drop) {
			if (characterGameObject == null) {
				if (!pickup) {
					pickedUpObject = null;
				}
				return;
			}

			Rigidbody rigidBody = null;
			if (pickup) {
				pickedUpObject = netWorld.GetPickupableGameObject(pickedUpObjectNetId);
				Client.Assert(pickedUpObject != null, "Player tried to pickup object that does not exists in world. Net id: " + pickedUpObjectNetId);
				oldPickupableLayer = pickedUpObject.layer;
				pickedUpObject.layer = Utils.LAYER_IGNORE_RAYCAST;
			}

			rigidBody = pickedUpObject.GetComponent<Rigidbody>();
			if (rigidBody != null) {
				rigidBody.isKinematic = pickup;
			}

			if (!pickup) {
				pickedUpObject.layer = oldPickupableLayer;
				pickedUpObject = null;
			}
		}
	}
}
