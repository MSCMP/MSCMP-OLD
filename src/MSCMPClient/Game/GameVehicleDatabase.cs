using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Objects;
using MSCMP.Game.Components;

namespace MSCMP.Game {
	/// <summary>
	/// Class handling the adding and removing of vehicles in the game.
	/// </summary>
	class GameVehicleDatabase : IGameObjectCollector {

		/// <summary>
		/// Singleton of the vehicle manager.
		/// </summary>
		public static GameVehicleDatabase Instance = null;

		/// <summary>
		/// List of AI vehicles and an ID to reference them by.
		/// </summary>
		public Dictionary<int, GameObject> vehiclesAI = new Dictionary<int, GameObject>();

		/// <summary>
		/// List of Player vehicles and an ID to reference them by.
		/// </summary>
		public Dictionary<int, GameObject> vehiclesPlayer = new Dictionary<int, GameObject>();

		public GameVehicleDatabase() {
			Instance = this;
		}

		~GameVehicleDatabase() {
			Instance = null;
		}

		/// <summary>
		/// Handle destroy of game object.
		/// </summary>
		/// <param name="gameObject">The destroyed game object.</param>
		public void DestroyObject(GameObject gameObject) {
			vehiclesAI.Clear();
		}

		/// <summary>
		/// Destroy all references to collected objects.
		/// </summary>
		public void DestroyObjects() {
			vehiclesAI.Clear();
		}

		/// <summary>
		/// Registers given gameObject as a vehicle if it's a vehicle.
		/// </summary>
		/// <param name="gameObject">The game object to check and eventually register.</param>
		public void CollectGameObject(GameObject gameObject) {
			// Player vehicles
			if (gameObject.name == "Colliders" && gameObject.transform.FindChild("CarCollider") != null || gameObject.name == "Colliders" && gameObject.transform.FindChild("Coll") != null) {
				if (vehiclesPlayer.ContainsValue(gameObject)) {
					Logger.Debug($"Duplicate Player vehicle prefab '{gameObject.name}' rejected");
				}
				else {
					vehiclesPlayer.Add(vehiclesPlayer.Count + 1, gameObject);
					Logger.Debug($"Registered Player vehicle prefab '{gameObject.transform.parent.name}' (Player Vehicle ID: {vehiclesPlayer.Count})");

					GameObject carCollider;
					if (gameObject.transform.FindChild("CarCollider") == null) {
						carCollider = gameObject.transform.FindChild("Coll").gameObject;
					}
					else {
						carCollider = gameObject.transform.FindChild("CarCollider").gameObject;
					}
					carCollider.gameObject.AddComponent<ObjectSyncComponent>().Setup(ObjectSyncManager.ObjectTypes.PlayerVehicle, ObjectSyncManager.AUTOMATIC_ID);
				}
			}

			if (gameObject.transform.FindChild("CarColliderAI") != null) {
				if (vehiclesAI.ContainsValue(gameObject)) {
					Logger.Debug($"Duplicate AI vehicle prefab '{gameObject.name}' rejected");
				}
				else {
					vehiclesAI.Add(vehiclesAI.Count + 1, gameObject);
					Logger.Debug($"Registered AI vehicle prefab '{gameObject.name}' (AI Vehicle ID: {vehiclesAI.Count})");

					GameObject carCollider = gameObject.transform.FindChild("CarColliderAI").gameObject;
					carCollider.gameObject.AddComponent<ObjectSyncComponent>().Setup(ObjectSyncManager.ObjectTypes.AIVehicle, ObjectSyncManager.AUTOMATIC_ID);
				}
			}
		}
	}
}
