using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using UnityEngine;

namespace MSCMP.Game {
	/// <summary>
	/// Database containing prefabs of all pickupables.
	/// </summary>
	class GamePickupableDatabase : IGameObjectCollector {
		static GamePickupableDatabase instance;
		public static GamePickupableDatabase Instance {
			get { return instance; }
		}

		/// <summary>
		/// All instances of gameobject pickupables.
		/// </summary>
		Dictionary<int, GameObject> pickupables = new Dictionary<int, GameObject>();

		/// <summary>
		/// Getter for pickupables.
		/// </summary>
		public Dictionary<int, GameObject> Pickupables {
			get { return pickupables; }
		}

		public GamePickupableDatabase() {
			instance = this;

			GameCallbacks.onPlayMakerObjectCreate +=
					(GameObject instance, GameObject prefab) => {
						PrefabDesc descriptor = GetPrefabDesc(prefab);
						if (descriptor != null) {
							var metaDataComponent =
									instance.AddComponent<Components.PickupableMetaDataComponent>();
							metaDataComponent.prefabId = descriptor.id;

							Logger.Log($"Pickupable has been spawned. ({instance.name})");
						}
					};
		}
		~GamePickupableDatabase() { instance = null; }

		/// <summary>
		/// Pickupable prefab descriptor.
		/// </summary>
		public class PrefabDesc {
			/// <summary>
			/// The unique id of the prefab.
			/// </summary>
			public int id;

			/// <summary>
			/// Prefab game object.
			/// </summary>
			public GameObject gameObject;

			/// <summary>
			/// Spawn new instance of the given pickupable at given world position.
			/// </summary>
			/// <param name="position">The position where to spawn pickupable at.</param>
			/// <param name="rotation">The rotation to apply on spawned pickupable.</param>
			/// <returns>Newly spawned pickupable game object.</returns>
			public GameObject Spawn(Vector3 position, Quaternion rotation) {
				// HACK: Jonnez is already spawned and there can be only one of it.
				// TODO: Get rid of it, it's ugly hack. Perhaps JONNEZ should behave like
				// pickupable.
				if (gameObject.name.StartsWith("JONNEZ ES")) {
					return GameObject.Find("JONNEZ ES(Clone)");
				}

				GameObject pickupable =
						(GameObject)Object.Instantiate(gameObject, position, rotation);
				pickupable.SetActive(true);
				pickupable.transform.SetParent(null);

				// Disable loading code on all spawned pickupables.

				PlayMakerFSM fsm = Utils.GetPlaymakerScriptByName(pickupable, "Use");
				if (fsm != null) {
					FsmState loadState = fsm.Fsm.GetState("Load");
					if (loadState != null) {
						var action = new SendEvent();
						action.eventTarget = new FsmEventTarget();
						action.eventTarget.excludeSelf = false;
						action.eventTarget.target = FsmEventTarget.EventTarget.Self;
						action.sendEvent = fsm.Fsm.GetEvent("FINISHED");
						PlayMakerUtils.AddNewAction(loadState, action);

						Logger.Log("Installed skip load hack for prefab " + pickupable.name);
					} else {
						Logger.Log("Failed to find state on " + pickupable.name);
					}
				}

				return pickupable;
			}
		}

		/// <summary>
		/// List containing prefabs.
		/// </summary>
		List<PrefabDesc> prefabs = new List<PrefabDesc>();

		/// <summary>
		/// Rebuild pickupables database.
		/// </summary>
		public void CollectGameObject(GameObject gameObject) {
			if (!IsPickupable(gameObject)) { return; }

			int prefabId = prefabs.Count;
			var metaDataComponent =
					gameObject.AddComponent<Components.PickupableMetaDataComponent>();
			metaDataComponent.prefabId = prefabId;

			PrefabDesc desc = new PrefabDesc();
			desc.gameObject = gameObject;
			desc.id = prefabId;

			// Activate game object if it's not active to make sure we can access all play
			// maker fsm.

			bool wasActive = desc.gameObject.activeSelf;
			if (!wasActive) { desc.gameObject.SetActive(true); }

			// Add ObjectSyncComponent.
			if (desc.gameObject.GetComponent<Components.ObjectSyncComponent>() == null) {
				desc.gameObject.AddComponent<Components.ObjectSyncComponent>().Setup(
						ObjectSyncManager.ObjectTypes.Pickupable, -1);
			} else {
				GameObject.Destroy(
						desc.gameObject.GetComponent<Components.ObjectSyncComponent>());
				desc.gameObject.AddComponent<Components.ObjectSyncComponent>().Setup(
						ObjectSyncManager.ObjectTypes.Pickupable, -1);
			}

			// Deactivate game object back if needed.

			if (!wasActive) { desc.gameObject.SetActive(false); }

			prefabs.Add(desc);

			Logger.Debug(
					$"Registered new prefab {gameObject.name} ({gameObject.GetInstanceID()}) into pickupable database. (Prefab ID: {prefabId})");
		}

		/// <summary>
		/// Handle collected objects destroy.
		/// </summary>
		public void DestroyObjects() { prefabs.Clear(); }

		/// <summary>
		/// Handle destroy of game object.
		/// </summary>
		/// <param name="gameObject">The destroyed game object.</param>
		public void DestroyObject(GameObject gameObject) {
			if (!IsPickupable(gameObject)) { return; }

			var prefab = GetPrefabDesc(gameObject);
			if (prefab != null) {
				Logger.Debug($"Deleting prefab descriptor - {gameObject.name}.");

				// Cannot use Remove() because GetPickupablePrefab() depends on indices to
				// stay untouched.
				prefabs[prefab.id] = null;
			}
		}

		/// <summary>
		/// Get pickupable prefab by it's id.
		/// </summary>
		/// <param name="prefabId">The id of the prefab to get.</param>
		/// <returns>The pickupable prefab descriptor.</returns>
		public PrefabDesc GetPickupablePrefab(int prefabId) {
			if (prefabId < prefabs.Count) { return prefabs[prefabId]; }
			return null;
		}

		/// <summary>
		/// Get prefab descriptor by prefab game object.
		/// </summary>
		/// <param name="prefab">The prefab game object.</param>
		/// <returns>Prefab descriptor if given prefab is valid.</returns>
		public PrefabDesc GetPrefabDesc(GameObject prefab) {
			foreach (var desc in prefabs) {
				if (desc != null && desc.gameObject == prefab) { return desc; }
			}
			return null;
		}

		/// <summary>
		/// Check if given game object is pickupable.
		/// </summary>
		/// <param name="gameObject">The game object to check.</param>
		/// <returns>true if given game object is pickupable, false otherwise</returns>
		static public bool IsPickupable(GameObject gameObject) {
			if (!gameObject.CompareTag("PART") && !gameObject.CompareTag("ITEM")) {
				return false;
			}

			if (!gameObject.GetComponent<Rigidbody>()) { return false; }
			return true;
		}
	}
}
