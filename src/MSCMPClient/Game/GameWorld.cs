using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Places;
using MSCMP.Game.Objects;
using MSCMP.Game.Components;
using HutongGames.PlayMaker;
using System.Text;

namespace MSCMP.Game {

	/// <summary>
	/// Object managing state of the game world.
	/// </summary>
	class GameWorld : IGameObjectCollector {
		public static GameWorld Instance = null;

		/// <summary>
		/// Doors manager.
		/// </summary>
		private GameDoorsManager doorsManager = new GameDoorsManager();

		/// <summary>
		/// Game pickupables database.
		/// </summary>
		GamePickupableDatabase gamePickupableDatabase = new GamePickupableDatabase();

		/// <summary>
		/// World time managing fsm.
		/// </summary>
		PlayMakerFSM worldTimeFsm = null;

		/// <summary>
		/// Light switch manager.
		/// </summary>
		LightSwitchManager lightSwitchManager = new LightSwitchManager();

		/// <summary>
		/// Weather manager.
		/// </summary>
		GameWeatherManager gameWeatherManager = new GameWeatherManager();

		/// <summary>
		/// Game vehicle database.
		/// </summary>
		GameVehicleDatabase gameVehicleDatabase = new GameVehicleDatabase();

		/// <summary>
		/// Object sync manager.
		/// </summary>
		ObjectSyncManager objectSyncManager = new ObjectSyncManager();

		/// <summary>
		/// Event hook.
		/// </summary>
		EventHook eventHook = new EventHook();

		private GamePlayer player = null;

		/// <summary>
		/// Get player game object.
		/// </summary>
		public GamePlayer Player {
			get { return player; }
		}

		private const string REFRESH_WORLD_TIME_EVENT = "MP_REFRESH_WORLD_TIME";

		float worldTimeCached = 0;

		/// <summary>
		/// Current world time. (hh.mm)
		/// </summary>
		public float WorldTime {
			set {
				// Make sure value is reasonable. (0 - 24 range)

				while (value > 24.0f) { value -= 24.0f; }

				// Make sure reported time is power of two..
				worldTimeCached = (float)((int)(value) / 2 * 2);

				if (worldTimeCached <= 2.0f) { worldTimeCached = 2.0f; }

				if (worldTimeFsm != null) {
					worldTimeFsm.Fsm.GetFsmInt("Time").Value = (int)worldTimeCached;
					worldTimeFsm.SendEvent(REFRESH_WORLD_TIME_EVENT);
				}
			}

			get {
				if (worldTimeFsm != null) {
					worldTimeCached = worldTimeFsm.Fsm.GetFsmInt("Time").Value;
				}
				return worldTimeCached;
			}
		}

		/// <summary>
		/// Current world day.
		/// </summary>
		public int WorldDay {
			get {
				return PlayMakerGlobals.Instance.Variables.GetFsmInt("GlobalDay").Value;
			}

			set {
				PlayMakerGlobals.Instance.Variables.GetFsmInt("GlobalDay").Value = value;
			}
		}

		/// <summary>
		/// Current Host in game last name.
		/// </summary>
		public string PlayerLastName {
			get { return lastnameTextMesh.text; }

			set {
				lastnameFSM.enabled = false;
				lastnameTextMesh.text = value;
			}
		}

		private TextMesh lastnameTextMesh = null;
		private PlayMakerFSM lastnameFSM = null;

		/// <summary>
		/// Setup red mailbox next to the player's home.
		/// </summary>
		public void SetupMailbox(GameObject mailboxGameObject) {
			lastnameTextMesh = mailboxGameObject.GetComponent<TextMesh>();
			lastnameFSM = mailboxGameObject.GetComponent<PlayMakerFSM>();
		}

		/// <summary>
		/// List containing all game objects collectors.
		/// </summary>
		List<IGameObjectCollector> gameObjectUsers = new List<IGameObjectCollector>();

		public GameWorld() {
			Instance = this;

			// Make sure game world will get notified about play maker
			// CreateObject/DestroyObject calls.

			GameCallbacks.onPlayMakerObjectCreate +=
					(GameObject instance, GameObject prefab) => { HandleNewObject(instance); };
			GameCallbacks.onPlayMakerObjectDestroy +=
					(GameObject instance) => { HandleObjectDestroy(instance); };

			// Register game objects users.

			gameObjectUsers.Add(this);
			gameObjectUsers.Add(doorsManager);
			gameObjectUsers.Add(gamePickupableDatabase);
			gameObjectUsers.Add(lightSwitchManager);
			gameObjectUsers.Add(gameWeatherManager);
			gameObjectUsers.Add(gameVehicleDatabase);
		}

		~GameWorld() { Instance = null; }

		/// <summary>
		/// The current game world hash.
		/// </summary>
		int worldHash = 0;

		/// <summary>
		/// Get unique world hash.
		/// </summary>
		public int WorldHash {
			get { return worldHash; }
		}

		/// <summary>
		/// Was game world has already generated?
		/// </summary>
		bool worldHashGenerated = false;

		/// <summary>
		/// Collect given objects.
		/// </summary>
		/// <param name="gameObject">The game object to collect.</param>
		public void CollectGameObject(GameObject gameObject) {
			if (gameObject.name == "SUN" && worldTimeFsm == null) {
				// Yep it's called "Color" :>
				worldTimeFsm = Utils.GetPlaymakerScriptByName(gameObject, "Color");
				if (worldTimeFsm == null) { return; }

				// Register refresh world time event.
				if (!worldTimeFsm.Fsm.HasEvent(REFRESH_WORLD_TIME_EVENT)) {
					FsmEvent mpRefreshWorldTimeEvent =
							worldTimeFsm.Fsm.GetEvent(REFRESH_WORLD_TIME_EVENT);
					PlayMakerUtils.AddNewGlobalTransition(
							worldTimeFsm, mpRefreshWorldTimeEvent, "State 1");
				}

				// Make sure world time is up-to-date with cache.
				WorldTime = worldTimeCached;
			} else if (Utils.IsGameObjectHierarchyMatching(
										 gameObject, "mailbox_bottom_player/Name")) {
				SetupMailbox(gameObject);
			} else if (gameObject.name == "TRAFFIC") {
				new TrafficManager(gameObject);
			} else if (gameObject.name == "STORE") {
				new Shop(gameObject);
			} else if (gameObject.name == "BOAT") {
				ObjectSyncComponent osc =
						gameObject.transform.FindChild("GFX")
								.FindChild("Colliders")
								.FindChild("Collider")
								.gameObject.AddComponent<ObjectSyncComponent>();
				osc.Setup(
						ObjectSyncManager.ObjectTypes.Boat, ObjectSyncManager.AUTOMATIC_ID);
			} else if (gameObject.name == "GarageDoors") {
				ObjectSyncComponent oscLeft =
						gameObject.transform.FindChild("DoorLeft")
								.FindChild("Coll")
								.gameObject.AddComponent<ObjectSyncComponent>();
				oscLeft.Setup(ObjectSyncManager.ObjectTypes.GarageDoor,
						ObjectSyncManager.AUTOMATIC_ID);
				ObjectSyncComponent oscRight =
						gameObject.transform.FindChild("DoorRight")
								.FindChild("Coll")
								.gameObject.AddComponent<ObjectSyncComponent>();
				oscRight.Setup(ObjectSyncManager.ObjectTypes.GarageDoor,
						ObjectSyncManager.AUTOMATIC_ID);
			}
		}

		/// <summary>
		/// Handle collected objects destroy.
		/// </summary>
		public void DestroyObjects() {
			worldTimeFsm = null;
			lastnameTextMesh = null;
			lastnameFSM = null;
		}

		/// <summary>
		/// Handle destroy of game object.
		/// </summary>
		/// <param name="gameObject">The destroyed game object.</param>
		public void DestroyObject(GameObject gameObject) {
			if (worldTimeFsm != null && worldTimeFsm.gameObject == gameObject) {
				worldTimeFsm = null;
			} else if (lastnameFSM != null && lastnameFSM.gameObject == gameObject) {
				lastnameFSM = null;
				lastnameTextMesh = null;
			}
		}

		/// <summary>
		/// Handle creation/load of new game object.
		/// </summary>
		/// <param name="gameObject">The new game object.</param>
		void HandleNewObject(GameObject gameObject) {
			foreach (IGameObjectCollector collector in gameObjectUsers) {
				collector.CollectGameObject(gameObject);
			}
		}

		/// <summary>
		/// Handle destroy of the given object.
		/// </summary>
		/// <param name="gameObject">Destroyed game object.</param>
		void HandleObjectDestroy(GameObject gameObject) {
			// Iterate backwards so pickupable users will be notified before the database.

			for (int i = gameObjectUsers.Count; i > 0; --i) {
				gameObjectUsers[i - 1].DestroyObject(gameObject);
			}
		}

		/// <summary>
		/// Callback called when world is loaded.
		/// </summary>
		public void OnLoad() {
			// Register all game objects.

			GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();

			foreach (GameObject go in gos) {
				if (!worldHashGenerated) {
					Transform transform = go.transform;
					while (transform != null) {
						worldHash ^= Utils.StringJenkinsHash(transform.name);
						transform = transform.parent;
					}
				}

				HandleNewObject(go);
			}

			Logger.Log("World hash: " + worldHash);
			worldHashGenerated = true;

			// Check mandatory objects.

			Client.Assert(worldTimeFsm != null, "No world time FSM found :(");
			Client.Assert(lastnameFSM != null, "Mailbox FSM couldn't be found!");
			Client.Assert(lastnameTextMesh != null, "Mailbox TextMesh couldn't be found!");

			// Notify different parts of the mod about the world load.

			if (GameCallbacks.onWorldLoad != null) { GameCallbacks.onWorldLoad(); }
		}

		/// <summary>
		/// Callback called when world gets unloaded.
		/// </summary>
		public void OnUnload() {
			// Iterate backwards so pickupable users will be notified before the database.

			for (int i = gameObjectUsers.Count; i > 0; --i) {
				gameObjectUsers[i - 1].DestroyObjects();
			}

			if (GameCallbacks.onWorldUnload != null) { GameCallbacks.onWorldUnload(); }

			player = null;
		}

		/// <summary>
		/// Update game world state.
		/// </summary>
		public void Update() {
			if (player == null) {
				var playerGo = GameObject.Find("PLAYER");

				if (playerGo != null) {
					player = new GamePlayer(playerGo);

					if (GameCallbacks.onLocalPlayerCreated != null) {
						GameCallbacks.onLocalPlayerCreated();
					}
				}
			}
		}

		/// <summary>
		/// List of vehicle gameobject names.
		/// </summary>
		static readonly string[] vehicleGoNames = { "JONNEZ ES(Clone)",
			"HAYOSIKO(1500kg, 250)", "SATSUMA(557kg, 248)", "RCO_RUSCKO12(270)",
			"KEKMET(350-400psi)", "FLATBED", "FERNDALE(1630kg)", "GIFU(750/450psi)" };

		public void UpdateIMGUI() {
			// noop
		}

		/// <summary>
		/// Spawns pickupable.
		/// </summary>
		/// <param name="prefabId">Pickupable prefab id.</param>
		/// <param name="position">The spawn position.</param>
		/// <param name="rotation">The spawn rotation.</param>
		/// <param name="objectID">The ObjectID of the object.</param>
		/// <returns>Spawned pickupable game object.</returns>
		public GameObject SpawnPickupable(
				int prefabId, Vector3 position, Quaternion rotation, int objectID) {
			GamePickupableDatabase.PrefabDesc prefabDescriptor =
					gamePickupableDatabase.GetPickupablePrefab(prefabId);
			Client.Assert(
					prefabDescriptor != null, $"Unable to find pickupable prefab {prefabId}");
			return prefabDescriptor.Spawn(position, rotation);
		}
	}
}
