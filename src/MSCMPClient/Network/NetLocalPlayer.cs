using System.IO;
using UnityEngine;

namespace MSCMP.Network {
	class NetLocalPlayer : NetPlayer {

		private float timeToUpdate = 0.0f;

		public NetLocalPlayer(NetManager netManager, Steamworks.CSteamID steamId) : base(netManager, steamId) {

		}

		public override void Update() {
			timeToUpdate -= Time.deltaTime;
			if (timeToUpdate <= 0.0f) {

				GameObject obj = GameObject.Find("PLAYER");
				Vector3 position = obj.transform.position;

				MemoryStream stream = new MemoryStream();
				BinaryWriter writer = new BinaryWriter(stream);

				writer.Write((byte)NetManager.PacketId.Synchronize);
				writer.Write((double)position.x);
				writer.Write((double)position.y);
				writer.Write((double)position.z);
				stream.Flush();

				netManager.SendPacket(stream.GetBuffer(), Steamworks.EP2PSend.k_EP2PSendUnreliable);

				timeToUpdate = 1.0f / 10.0f;
			}
		}
	}
}
