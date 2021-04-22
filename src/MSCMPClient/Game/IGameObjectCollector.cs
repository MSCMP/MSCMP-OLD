using UnityEngine;

namespace MSCMP.Game {
	interface IGameObjectCollector {

		/// <summary>
		/// Called when there is a new game object that can be collected.
		/// </summary>
		/// <remarks>Remember that this method may be called multiple times for the same
		/// game object!</remarks> <param name="gameObject">The game object that can be
		/// collected.</param>
		void CollectGameObject(GameObject gameObject);

		/// <summary>
		/// Called when all collected objects are destroyed.
		/// </summary>
		void DestroyObjects();

		/// <summary>
		/// Called when given game object is destroyed.
		/// </summary>
		/// <param name="gameObject">Destroyed game object.</param>
		void DestroyObject(GameObject gameObject);
	}
}
