using System;
using UnityEngine;

namespace MSCMP.Network {

	/// <summary>
	/// Class representing network player.
	/// </summary>
	class NetPlayer : IDisposable {

		private Steamworks.CSteamID steamId = Steamworks.CSteamID.Nil;

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

					MPController.logFile.WriteLine("REMOVED " + name + " FSM!");
				}
			};

			RemoveFSM("Move"); // Performs random character moves.
			RemoveFSM("Obstacle"); // Rotates character to side to not walk "to the wall" when touching it.
			RemoveFSM("Raycast"); // Snaps character to ground.
			RemoveFSM("CarHit"); // Spawns ragdoll when character gets hit by car.


			Animation anim = characterGameObject.GetComponentInChildren<Animation>();
			if (anim != null) {
				// TODO: SOME DEBUG SHIT HERE

				MPController.logFile.WriteLine("Have animation component! " + anim.GetClipCount());


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
		/// Updates state of the player.
		/// </summary>
		public virtual void Update() {

			// Some naive interpolation.

			if (characterGameObject && syncReceiveTime > 0) {
				float progress = (float)(netManager.GetNetworkClock() - syncReceiveTime) / INTERPOLATION_TIME;
				if (progress >= 2.0f) {
					return;
				}

				currentPos = Vector3.Lerp(sourcePos, targetPos, progress);
				currentRot = Quaternion.Slerp(sourceRot, targetRot, progress);

				characterGameObject.transform.position = currentPos;
				characterGameObject.transform.rotation = currentRot;
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
				"progress =  " + progress + "\n"
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
