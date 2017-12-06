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

		private GameObject playerGameObject = null;

		/// <summary>
		/// Get player game object.
		/// </summary>
		public GameObject PlayerGameObject {
			get {
				return playerGameObject;
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
			doorsManager.OnWorldLoad();
			LoadVehicles();
		}

		/// <summary>
		/// Callback called when world gets unloaded.
		/// </summary>
		public void OnUnload() {
			vehicles.Clear();
			playerGameObject = null;
		}

		/// <summary>
		/// Update game world state.
		/// </summary>
		public void Update() {
			if (playerGameObject == null) {
				playerGameObject = GameObject.Find("PLAYER");
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
	}
}
