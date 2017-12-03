using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Objects;

namespace MSCMP.Game {
	/// <summary>
	/// Class managing state of the doors in game.
	/// </summary>
	class GameDoorsManager {

		/// <summary>
		/// Singleton of the doors manager.
		/// </summary>
		public static GameDoorsManager Instance = null;

		/// <summary>
		/// List of the doors.
		/// </summary>
		public List<GameDoor> doors = new List<GameDoor>();

		public delegate void OnDoorsOpen();
		public delegate void OnDoorsClose();

		/// <summary>
		/// Callback called when local player opens any doors.
		/// </summary>
		public OnDoorsOpen onDoorsOpen;

		/// <summary>
		/// Callback called when local players closes any doors.
		/// </summary>
		public OnDoorsClose onDoorsClose;

		public GameDoorsManager() {
			Instance = this;
		}

		~GameDoorsManager() {
			Instance = null;
		}

		/// <summary>
		/// Builds doors list on world load.
		/// </summary>
		public void OnWorldLoad() {
			doors.Clear();
			GameObject []gos = GameObject.FindObjectsOfType<GameObject>();

			// Register all doors in game.

			foreach (var go in gos) {

				if (!go.name.StartsWith("Door")) {
					continue;
				}


				if (go.transform.childCount == 0) {
					continue;
				}

				Transform pivot = go.transform.GetChild(0);
				if (pivot == null || pivot.name != "Pivot") {
					continue;
				}

				var playMakerFsm = Utils.GetPlaymakerScriptByName(go, "Use");
				if (playMakerFsm == null) {
					continue;
				}

				bool isValid = false;
				foreach (var e in playMakerFsm.FsmEvents) {
					if (e.Name == "OPENDOOR") {
						isValid = true;
						break;
					}
				}

				if (isValid) {
					GameDoor door = new GameDoor(go);
					door.onOpen = () => {
						onDoorsOpen();
					};
					door.onClose = () => {
						onDoorsClose();
					};
					doors.Add(door);

					MPController.logFile.WriteLine("Registered doors " + go.name);
				}
			}
		}

		/// <summary>
		/// Find doors at given world location.
		/// </summary>
		/// <param name="position">The location of the doors.</param>
		/// <returns></returns>
		public GameDoor FindGameDoors(Vector3 position) {
			float closestDistance = 5.0f;
			GameDoor closestDoors = null;
			foreach (var door in doors) {
				float distance = door.DistanceToPoint(position);
				if (distance > closestDistance) {
					continue;
				}

				closestDistance = distance;
				closestDoors = door;
			}
			return closestDoors;
		}
	}
}
