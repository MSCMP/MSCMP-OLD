
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
				}
			}
		}
		public virtual void DrawDebugGUI() {
			GUI.Label(new Rect(300, 200, 300, 200), "Remote player (position: " + pos.ToString() + ")");
		}

		public void HandleSynchronize(BinaryReader reader) {
			pos.x = (float)reader.ReadDouble();
			pos.y = (float)reader.ReadDouble();
			pos.z = (float)reader.ReadDouble();

			rot.w = (float)reader.ReadDouble();
			rot.x = (float)reader.ReadDouble();
			rot.y = (float)reader.ReadDouble();
			rot.z = (float)reader.ReadDouble();

			if (! go) {
				return;
			}

			go.transform.position = pos;
			go.transform.rotation = rot;
		}
	}
}
