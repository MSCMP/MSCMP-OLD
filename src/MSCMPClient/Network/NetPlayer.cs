using MSCMP.Game;
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


		public Vector3 currentPos = new Vector3();
		public Quaternion currentRot = new Quaternion();

		public Vector3 sourcePos = new Vector3();
		public Quaternion sourceRot = new Quaternion();
		public Vector3 targetPos = new Vector3();
		public Quaternion targetRot = new Quaternion();

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
		/// Constructor.
		/// </summary>
		/// <param name="netManager">Network manager managing connection to the player.</param>
		/// <param name="steamId">Player's steam id.</param>
		public NetPlayer(NetManager netManager, Steamworks.CSteamID steamId) {
			this.netManager = netManager;
			this.steamId = steamId;
		}

		/// <summary>
		/// Spawns character object in world.
		/// </summary>
		public void Spawn() {
			GameObject prefab = GameObject.Find(CHARACTER_PREFAB_NAME);

			characterGameObject = (GameObject)GameObject.Instantiate((GameObject)prefab, currentPos, currentRot);

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
					Vector3 oldPos = currentPos;
					currentPos = Vector3.Lerp(sourcePos, targetPos, progress);
					currentRot = Quaternion.Slerp(sourceRot, targetRot, progress);
					Vector3 delta = (currentPos - oldPos);
					delta.y = 0.0f;
					speed = delta.magnitude;

					characterGameObject.transform.position = currentPos + CHARACTER_OFFSET;
					characterGameObject.transform.rotation = currentRot;
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
			float progress = (float)(netManager.GetNetworkClock() - syncReceiveTime) / INTERPOLATION_TIME;
			GUI.Label(new Rect(300, 200, 300, 200), "Remote player\ncurrentPos = " + currentPos.ToString() + "\n" +
				"sourcePos = " + sourcePos.ToString() + "\n" +
				"targetPos =  " + targetPos.ToString() + "\n" +
				"progress =  " + progress + "\n" +
				"anim = " + currentAnim + "\n" +
				"animSpeed = " + activeAnimationState.speed + "\n"
			);
		}
#endif

		/// <summary>
		/// Handle received synchronization message.
		/// </summary>
		/// <param name="msg">The received synchronization message.</param>
		public void HandleSynchronize(Messages.PlayerSyncMessage msg) {
			targetPos.x = msg.position.x;
			targetPos.y = msg.position.y;
			targetPos.z = msg.position.z;

			targetRot.w = msg.rotation.w;
			targetRot.x = msg.rotation.x;
			targetRot.y = msg.rotation.y;
			targetRot.z = msg.rotation.z;

			sourcePos = currentPos;
			sourceRot = currentRot;

			syncReceiveTime = netManager.GetNetworkClock();

			if (!characterGameObject) {
				currentPos = targetPos;
				currentRot = targetRot;
				return;
			}
		}
	}
}
