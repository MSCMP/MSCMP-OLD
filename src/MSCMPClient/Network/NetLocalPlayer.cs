using System.IO;
using UnityEngine;

namespace MSCMP.Network {
	class NetLocalPlayer : NetPlayer {

		private float timeToUpdate = 0.0f;

		public NetLocalPlayer(NetManager netManager, Steamworks.CSteamID steamId) : base(netManager, steamId) {

		}
		public override void DrawDebugGUI() {
			GUI.Label(new Rect(300, 10, 300, 200), "Local player (time to update: " + timeToUpdate + ")");
		}


		public override void Update() {
			timeToUpdate -= Time.deltaTime;
			if (timeToUpdate <= 0.0f) {

				GameObject obj = GameObject.Find("PLAYER");
				if (!obj) return;
				if (!obj.transform) return;
				Vector3 position = obj.transform.position;
				Quaternion rotation = obj.transform.rotation;
				MemoryStream stream = new MemoryStream();
				BinaryWriter writer = new BinaryWriter(stream);

				writer.Write((byte)NetManager.PacketId.Synchronize);
				writer.Write((double)position.x);
				writer.Write((double)position.y);
				writer.Write((double)position.z);
				writer.Write((double)rotation.w);
				writer.Write((double)rotation.x);
				writer.Write((double)rotation.y);
				writer.Write((double)rotation.z);
				stream.Flush();

				netManager.SendPacket(stream.GetBuffer(), Steamworks.EP2PSend.k_EP2PSendUnreliable);

				timeToUpdate = 1.0f / 10.0f;
			}
		}
	}
}
