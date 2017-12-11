using UnityEngine;

namespace MSCMP.Game {
	/// <summary>
	/// Various callbacks called on game actions.
	/// </summary>
	static class GameCallbacks {
		/// <summary>
		/// Delegate of callback called when local player pickups object.
		/// </summary>
		/// <param name="gameObj">Picked up game object.</param>
		public delegate void OnObjectPickup(GameObject gameObj);

		/// <summary>
		/// Callback called when local player pickups object.
		/// </summary>
		static public OnObjectPickup onObjectPickup;

		/// <summary>
		/// Delegate of callback called when local player pickups object.
		/// </summary>
		/// <param name="drop">Was the object dropped or throwed?</param>
		public delegate void OnObjectRelease(bool drop);

		/// <summary>
		/// Callback called when local player releases object.
		/// </summary>
		static public OnObjectRelease onObjectRelease;

		public delegate void OnLocalPlayerCreated();

		/// <summary>
		/// Callback called when local player spawns.
		/// </summary>
		static public OnLocalPlayerCreated onLocalPlayerCreated;

		public delegate void OnWorldLoad();

		/// <summary>
		/// Callback called when game world gets loaded.
		/// </summary>
		static public OnWorldLoad onWorldLoad;

		public delegate void OnWorldUnload();

		/// <summary>
		/// Callback called when game world gets unloaded.
		/// </summary>
		static public OnWorldUnload onWorldUnload;
	}
}
