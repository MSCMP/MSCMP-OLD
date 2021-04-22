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

#pragma warning disable CS0649 // Temporarily disable CS0649 warning as
															 // onLocalPlayerCreated is never used.
		/// <summary>
		/// Callback called when local player spawns.
		/// </summary>
		static public OnLocalPlayerCreated onLocalPlayerCreated;
#pragma warning restore CS0649

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

		/// <summary>
		/// Delegate of the callback called when PlayMaker creates new object.
		/// </summary>
		/// <param name="instance">The instance of the new object.</param>
		/// <param name="prefab">The prefab used to instantiate this object.</param>
		public delegate void OnPlayMakerObjectCreate(
				GameObject instance, GameObject prefab);

		/// <summary>
		/// Callback called when PlayMaker creates new object.
		/// </summary>
		static public OnPlayMakerObjectCreate onPlayMakerObjectCreate = null;

		/// <summary>
		/// Delegate of the callback called when PlayMaker destroys object.
		/// </summary>
		/// <param name="instance">The instance of the object that will be
		/// destroyed.</param>
		public delegate void OnPlayMakerObjectDestroy(GameObject instance);

		/// <summary>
		/// Callback called when PlayMaker destroys object.
		/// </summary>
		static public OnPlayMakerObjectDestroy onPlayMakerObjectDestroy = null;

		/// <summary>
		/// Delegate of the callback called when PlayMaker activates game object.
		/// </summary>
		/// <param name="instance">The instance of the object that will be
		/// activated/deactivated.</param> <param name="activate">Is the object
		/// activating?</param>
		public delegate void OnPlayMakerObjectActivate(
				GameObject instance, bool activate);

		/// <summary>
		/// Callback called when PlayMaker activates object.
		/// </summary>
		static public OnPlayMakerObjectActivate onPlayMakerObjectActivate = null;

		/// <summary>
		/// Delegate of the callback called when PlayMaker sets position of the game
		/// object.
		/// </summary>
		/// <param name="instance">The instance of the object this event is
		/// about.</param> <param name="position">The position to set.</param> <param
		/// name="space">The position space.</param>
		public delegate void OnPlayMakerSetPosition(
				GameObject instance, Vector3 newPosition, Space space);

		/// <summary>
		/// Callback called when PlayMaker sets position of an object.
		/// </summary>
		static public OnPlayMakerSetPosition onPlayMakerSetPosition = null;
	}
}
