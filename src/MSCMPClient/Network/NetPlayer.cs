using System;
using UnityEngine;

namespace MSCMP.Network {
	class NetPlayer : IDisposable {

		private Steamworks.CSteamID steamId = Steamworks.CSteamID.Nil;
		public Steamworks.CSteamID SteamId {
			get { return steamId; }
		}

		protected NetManager netManager = null;

		private GameObject go = null;
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

		private bool spawned = true;

		public NetPlayer(NetManager netManager, Steamworks.CSteamID steamId) {
			this.netManager = netManager;
			this.steamId = steamId;
		}

		public void Spawn() {
			spawned = true;
		}

		public void Dispose() {
			// Destroy player model on disconnect/timeout.

			if (go != null) {
				GameObject.Destroy(go);
				go = null;
			}
		}

		public bool SendPacket(byte[] data, Steamworks.EP2PSend sendType, int channel = 0) {
			return Steamworks.SteamNetworking.SendP2PPacket(this.steamId, data, (uint)data.Length, sendType, channel);
		}

		public virtual void Update() {
			// uugllglglgy as fuck
			if (spawned && !go) {
				GameObject prefab = GameObject.Find("Hullu");
				if (prefab) {
					go = (GameObject)GameObject.Instantiate((GameObject)prefab, currentPos, currentRot);
					GameObject.DontDestroyOnLoad(go);

					Action<string> RemoveFSM = (string name) => {
						PlayMakerFSM fsm = Utils.GetPlaymakerScriptByName(go, name);
						if (fsm != null) {
							GameObject.Destroy(fsm);

							MPController.logFile.WriteLine("REMOVED " + name + " FSM!");
						}
					};
					RemoveFSM("Move");
					RemoveFSM("Obstacle");
					RemoveFSM("Raycast");


					Animation anim = go.GetComponent<Animation>();
					if (anim != null) {
						// TODO: BULLSHIT HERE

						MPController.logFile.WriteLine("Have animation component! " + anim.GetClipCount());


					}
				}
			}

			// Some naive interpolation.

			if (spawned && go && syncReceiveTime > 0) {
				float progress = (float)(netManager.GetNetworkClock() - syncReceiveTime) / INTERPOLATION_TIME;
				if (progress >= 2.0f) {
					return;
				}

				currentPos = Vector3.Lerp(sourcePos, targetPos, progress);
				currentRot = Quaternion.Slerp(sourceRot, targetRot, progress);

				go.transform.position = currentPos;
				go.transform.rotation = currentRot;
			}
		}
		public virtual void DrawDebugGUI() {
			float progress = (float)(netManager.GetNetworkClock() - syncReceiveTime) / INTERPOLATION_TIME;
			GUI.Label(new Rect(300, 200, 300, 200), "Remote player\ncurrentPos = " + currentPos.ToString() + "\n" +
				"sourcePos = " + sourcePos.ToString() + "\n" +
				"targetPos =  " + targetPos.ToString() + "\n" +
				"progress =  " + progress + "\n"
			);

		}

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

			if (! go) {
				currentPos = targetPos;
				currentRot = targetRot;
				return;
			}
		}
	}
}
