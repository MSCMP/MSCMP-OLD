using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Objects;

namespace MSCMP.Game {

	/// <summary>
	/// Object managing state of the game world.
	/// </summary>
	class GameWorld {


		public static GameWorld Instance = null;

		/// <summary>
		/// Doors manager.
		/// </summary>
		private GameDoorsManager doorsManager = new GameDoorsManager();

		/// <summary>
		/// List containing game vehicles.
		/// </summary>
		private List<GameVehicle> vehicles = new List<GameVehicle>();

		/// <summary>
		/// Game animations database.
		/// </summary>
		GameAnimDatabase gameAnimDatabase = new GameAnimDatabase();

		/// <summary>
		/// Game pickupables database.
		/// </summary>
		GamePickupableDatabase gamePickupableDatabase = new GamePickupableDatabase();

		/// <summary>
		/// World time managing fsm.
		/// </summary>
		PlayMakerFSM worldTimeFsm = null;

		private GamePlayer player = null;

		/// <summary>
		/// Get player game object.
		/// </summary>
		public GamePlayer Player {
			get {
				return player;
			}
		}

		float worldTimeCached = 0;

		/// <summary>
		/// Current world time. (hh.mm)
		/// </summary>
		public float WorldTime {
			set {
				worldTimeCached = value;
				if (worldTimeFsm != null) {
					int hours = (int)value;
					worldTimeFsm.Fsm.GetFsmInt("Time").Value = hours;
					worldTimeFsm.Fsm.GetFsmFloat("Minutes").Value = value - hours;
				}
			}

			get {
				if (worldTimeFsm != null) {
					worldTimeCached = worldTimeFsm.Fsm.GetFsmInt("Time").Value;
					worldTimeCached += worldTimeFsm.Fsm.GetFsmFloat("Minutes").Value;
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



		public GameWorld() {
			Instance = this;
		}

		~GameWorld() {
			Instance = null;
		}

		/// <summary>
		/// Callback called when world is loaded.
		/// </summary>
		public void OnLoad() {
			// Cache world time management fsm.

			GameObject sunGameObject = GameObject.Find("SUN");
			Client.Assert(sunGameObject != null, "SUN game object is missing!");

			// Yep it's called "Color" :>
			worldTimeFsm = Utils.GetPlaymakerScriptByName(sunGameObject, "Color");
			Client.Assert(worldTimeFsm != null, "Now world time FSM found :(");

			// Make sure world time is up-to-date with cache.
			WorldTime = worldTimeCached;

			gameAnimDatabase.Rebuild();
			gamePickupableDatabase.Rebuild();

			doorsManager.OnWorldLoad();
			LoadVehicles();

			if (GameCallbacks.onWorldLoad != null) {
				GameCallbacks.onWorldLoad();
			}
		}

		/// <summary>
		/// Callback called when world gets unloaded.
		/// </summary>
		public void OnUnload() {
			worldTimeFsm = null;

			if (GameCallbacks.onWorldUnload != null) {
				GameCallbacks.onWorldUnload();
			}

			vehicles.Clear();
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
		/// Load game vehicles and create game objects for them.
		/// </summary>
		private void LoadVehicles() {
			vehicles.Clear();

			// Register all vehicles.

			vehicles.Add(new GameVehicle(GameObject.Find("JONNEZ ES(Clone)")));
			vehicles.Add(new GameVehicle(GameObject.Find("HAYOSIKO(1500kg, 250)")));
			vehicles.Add(new GameVehicle(GameObject.Find("SATSUMA(557kg, 248)")));
			vehicles.Add(new GameVehicle(GameObject.Find("RCO_RUSCKO12(270)")));
			vehicles.Add(new GameVehicle(GameObject.Find("KEKMET(350-400psi)")));
			vehicles.Add(new GameVehicle(GameObject.Find("FLATBED")));
			vehicles.Add(new GameVehicle(GameObject.Find("FERNDALE(1630kg)")));
		}

		public GameVehicle FindVehicleByName(string name) {
			foreach (var veh in vehicles) {
				if (veh.Name == name) {
					return veh;
				}
			}
			return null;
		}

		public void UpdateIMGUI() {
			foreach (var v in vehicles) {
				v.UpdateIMGUI();
			}
		}

		/// <summary>
		/// Spawns pickupable.
		/// </summary>
		/// <param name="prefabId">Pickupable prefab id.</param>
		/// <param name="position">The spawn position.</param>
		/// <param name="rotation">The spawn rotation.</param>
		/// <returns>Spawned pickupable game object.</returns>
		public GameObject SpawnPickupable(int prefabId, Vector3 position, Quaternion rotation) {
			GamePickupableDatabase.PrefabDesc prefabDescriptor = gamePickupableDatabase.GetPickupablePrefab(prefabId);
			Client.Assert(prefabDescriptor != null, $"Unable to find pickupable prefab {prefabId}");
			return prefabDescriptor.Spawn(position, rotation);
		}
	}
}
