using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Objects;

namespace MSCMP.Game {
	/// <summary>
	/// Class managing state of the light switches in game.
	/// </summary>
	class LightSwitchManager : IGameObjectCollector {
		/// <summary>
		/// Singleton of the light switch manager.
		/// </summary>
		public static LightSwitchManager Instance = null;

		/// <summary>
		/// List of light switches.
		/// </summary>
		public List<LightSwitch> lightSwitches = new List<LightSwitch>();

		public delegate void OnLightSwitchUsed(GameObject lightSwitch, bool turnedOn);

		/// <summary>
		/// Callback called when player used a light switch
		/// </summary>
		public OnLightSwitchUsed onLightSwitchUsed;

		public LightSwitchManager() {
			Instance = this;
		}

		~LightSwitchManager() {
			Instance = null;
		}

		/// <summary>
		/// Check if given game object is a light switch.
		/// </summary>
		/// <param name="gameObject">Game object to check.</param>
		/// <returns>true if given game object is light switch, false otherwise</returns>
		bool IsLightSwitch(GameObject gameObject) {
			return gameObject.name.StartsWith("switch_");
		}

		/// <summary>
		/// Collect light switches.
		/// </summary>
		public void CollectGameObject(GameObject gameObject) {
			if (IsLightSwitch(gameObject) && GetLightSwitchByGameObject(gameObject) == null) {
				AddLightSwitch(gameObject);
			}
		}

		/// <summary>
		/// Destroy all collected objects references.
		/// </summary>
		public void DestroyObjects() {
			lightSwitches.Clear();
		}

		/// <summary>
		/// Handle destroy of game object.
		/// </summary>
		/// <param name="gameObject">The destroyed game object.</param>
		public void DestroyObject(GameObject gameObject) {
			if (IsLightSwitch(gameObject)) {
				var lightSwitch = GetLightSwitchByGameObject(gameObject);
				if (lightSwitch != null) {
					lightSwitches.Remove(lightSwitch);
				}
			}
		}

		/// <summary>
		/// Adds light switches by GameObject
		/// </summary>
		/// <param name="lightGO">LightSwitch GameObject.</param>
		public void AddLightSwitch(GameObject lightGO) {
			PlayMakerFSM playMakerFsm = Utils.GetPlaymakerScriptByName(lightGO, "Use");
			if (playMakerFsm == null) {
				return;
			}

			bool isValid = false;
			if (playMakerFsm.FsmVariables.FindFsmBool("Switch") != null) {
				isValid = true;
			}

			if (isValid) {
				LightSwitch light = new LightSwitch(lightGO);
				lightSwitches.Add(light);
				Logger.Log($"Registered new light switch: {lightGO.name}");

				light.onLightSwitchUse = (lightObj, turnedOn) => {
					onLightSwitchUsed(lightGO, !light.SwitchStatus);
				};
			}
		}

		/// <summary>
		/// Find light switch from position
		/// </summary>
		/// <param name="name">Light switch position.</param>
		/// <returns></returns>
		public LightSwitch FindLightSwitch(Vector3 pos) {
			foreach (LightSwitch light in lightSwitches) {
				if ((Vector3.Distance(light.Position, pos) < 0.1f)) {
					return light;
				}
			}
			return null;
		}

		/// <summary>
		/// Get light switch by game object.
		/// </summary>
		/// <param name="gameObject">The game object to get lightswitch by.</param>
		/// <returns>Lightswitch instance or null if there is no lightswitch matching this game object.</returns>
		public LightSwitch GetLightSwitchByGameObject(GameObject gameObject) {
			foreach (var light in lightSwitches) {
				if (light.GameObject == gameObject) {
					return light;
				}
			}
			return null;
		}
	}
}
