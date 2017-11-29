
using System.IO;
using UnityEngine;

namespace MSCMP.Network {
	class NetPlayer {

		private Steamworks.CSteamID steamId = Steamworks.CSteamID.Nil;
		public Steamworks.CSteamID SteamId {
			get { return steamId; }
		}

		protected NetManager netManager = null;

		private GameObject go = null;
		public bool hasHandshake = false;

		public Vector3 pos = new Vector3();
		public Quaternion rot = new Quaternion();

		private bool spawned = true;

		public NetPlayer(NetManager netManager, Steamworks.CSteamID steamId) {
			this.netManager = netManager;
			this.steamId = steamId;
		}

		public void Spawn() {
			spawned = true;
		}

		public bool SendPacket(byte[] data, Steamworks.EP2PSend sendType, int channel = 0) {
			return Steamworks.SteamNetworking.SendP2PPacket(this.steamId, data, (uint)data.Length, sendType, channel);
		}

		public virtual void Update() {
			// uugllglglgy as fuck
			if (spawned && !go) {
				GameObject prefab = GameObject.Find("Hullu");
				if (prefab) {
					go = (GameObject)GameObject.Instantiate((GameObject)prefab, pos, rot);
					GameObject.DontDestroyOnLoad(go);

					// Remove walking fsm (if exists)
					PlayMakerFSM fsm = Utils.GetPlaymakerScriptByName(go, "Move");
					if (fsm != null) {
						GameObject.Destroy(fsm);

						MPController.logFile.WriteLine("REMOVED Move FSM!");
					}
					fsm = Utils.GetPlaymakerScriptByName(go, "Obstacle");
					if (fsm != null) {
						GameObject.Destroy(fsm);

						MPController.logFile.WriteLine("REMOVED Obstacle FSM!");
					}


					Animation anim = go.GetComponent<Animation>();
					if (anim != null) {
						// TODO: BULLSHIT HERE

						MPController.logFile.WriteLine("Have animation component! " + anim.GetClipCount());


					}
				}
			}
		}
		public virtual void DrawDebugGUI() {
			GUI.Label(new Rect(300, 200, 300, 200), "Remote player (position: " + pos.ToString() + ")");
		}

		public void HandleSynchronize(Messages.PlayerSyncMessage msg) {
			pos.x = msg.position.x;
			pos.y = msg.position.y;
			pos.z = msg.position.z;

			rot.w = msg.rotation.w;
			rot.x = msg.rotation.x;
			rot.y = msg.rotation.y;
			rot.z = msg.rotation.z;

			if (! go) {
				return;
			}

			go.transform.position = pos;
			go.transform.rotation = rot;
		}
	}
}
