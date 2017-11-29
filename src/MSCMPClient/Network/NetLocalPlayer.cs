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

				Messages.PlayerSyncMessage message = new Messages.PlayerSyncMessage();
				message.position.x = position.x;
				message.position.y = position.y;
				message.position.z = position.z;

				message.rotation.w = rotation.w;
				message.rotation.x = rotation.x;
				message.rotation.y = rotation.y;
				message.rotation.z = rotation.z;
				netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendUnreliable);

				timeToUpdate = 1.0f / 10.0f;
			}
		}
	}
}
