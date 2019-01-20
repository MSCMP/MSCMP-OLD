using System;
using UnityEngine;
using MSCMP.Game;
using MSCMP.Game.Objects;

namespace MSCMP.Game {
	/// <summary>
	/// Manages traffic related triggers and provides waypoints for traffic navigation.
	/// </summary>
	class TrafficManager {
		GameObject traffic;
		public static GameObject routes;

		/// <summary>
		/// Constructor.
		/// </summary>
		public TrafficManager(GameObject trafficGo) {
			traffic = trafficGo;
			routes = traffic.transform.FindChild("Routes").gameObject;

			GameObject triggerManager = traffic.transform.FindChild("TriggerManager").gameObject;

			PlayMakerFSM[] fsms = triggerManager.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				EventHook.SyncAllEvents(fsm);
			}
		}

		/// <summary>
		/// Get the GameObject of a waypoint.
		/// </summary>
		/// <param name="waypoint">Waypoint's name, as an int.</param>
		/// <returns>Waypoint GameObject.</returns>
		public static GameObject GetWaypoint(float waypoint, int route) {
			GameObject waypointGo = null;

			switch (route) {
				// BusRoute
				case 0:
					waypointGo = routes.transform.FindChild("BusRoute").FindChild("" + waypoint).gameObject;
					break;
				// DirtRoad
				case 1:
					waypointGo = routes.transform.FindChild("DirtRoad").FindChild("" + waypoint).gameObject;
					break;
				// Highway
				case 2:
					waypointGo = routes.transform.FindChild("Highway").FindChild("" + waypoint).gameObject;
					break;
				// HomeRoad
				case 3:
					waypointGo = routes.transform.FindChild("HomeRoad").FindChild("" + waypoint).gameObject;
					break;
				// RoadRace
				case 4:
					waypointGo = routes.transform.FindChild("RoadRace").FindChild("" + waypoint).gameObject;
					break;
				// Trackfield
				case 5:
					waypointGo = routes.transform.FindChild("Trackfield").FindChild("" + waypoint).gameObject;
					break;
				// Village
				case 6:
					waypointGo = routes.transform.FindChild("Village").FindChild("" + waypoint).gameObject;
					break;
			}

			if (waypointGo == null) {
				Logger.Log($"Couldn't find waypoint, waypoint: {waypoint}, route: {route}");
			}

			return waypointGo;
		}
	}
}
