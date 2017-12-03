using MSCMP.Game;
using UnityEngine;

namespace MSCMP.Network {

	/// <summary>
	/// Class handling local player state.
	/// </summary>
	class NetLocalPlayer : NetPlayer {

		/// <summary>
		/// How much time in seconds left until next synchronization packet will be sent.
		/// </summary>
		private float timeToUpdate = 0.0f;

		/// <summary>
		/// Synchronization interval in miliseconds.
		/// </summary>
		public const ulong SYNC_INTERVAL = 100;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">The network manager owning this player.</param>
		/// <param name="steamId">The steam id of the player.</param>
		public NetLocalPlayer(NetManager netManager, Steamworks.CSteamID steamId) : base(netManager, steamId) {

			GameDoorsManager.Instance.onDoorsOpen = () => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.open = true;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);

				MPController.logFile.WriteLine("OPEN");
			};

			GameDoorsManager.Instance.onDoorsClose = () => {
				Messages.OpenDoorsMessage msg = new Messages.OpenDoorsMessage();
				msg.open = false;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);

				MPController.logFile.WriteLine("CLOSE");
			};
		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Update debug IMGUI for the player.
		/// </summary>
		public override void DrawDebugGUI() {
			GUI.Label(new Rect(300, 10, 300, 200), "Local player (time to update: " + timeToUpdate + ")");
		}
#endif

		/// <summary>
		/// Update state of the local player.
		/// </summary>
		public override void Update() {
			// Synchronization sending.

			timeToUpdate -= Time.deltaTime;
			if (timeToUpdate <= 0.0f && netManager.IsPlaying) {

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

				timeToUpdate = (float)SYNC_INTERVAL / 1000.0f;
			}
		}
	}
}
