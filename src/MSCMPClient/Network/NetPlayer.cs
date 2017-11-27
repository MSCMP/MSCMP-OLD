
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

		public NetPlayer(NetManager netManager, Steamworks.CSteamID steamId) {
			this.netManager = netManager;
			this.steamId = steamId;
		}

		public void Spawn() {
			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		}

		public bool SendPacket(byte[] data, Steamworks.EP2PSend sendType, int channel = 0) {
			return Steamworks.SteamNetworking.SendP2PPacket(this.steamId, data, (uint)data.Length, sendType, channel);
		}

		public virtual void Update() {

		}

		public void HandleSynchronize(BinaryReader reader) {
			Vector3 newPos = new Vector3();
			newPos.x = (float)reader.ReadDouble();
			newPos.y = (float)reader.ReadDouble();
			newPos.z = (float)reader.ReadDouble();
			this.go.transform.position = newPos;
		}
	}
}
