using UnityEngine;
using System.Text;
using System.IO;
using System;

namespace MSCMP {
#if !PUBLIC_RELEASE
	/// <summary>
	/// Development tools.
	/// </summary>
	static class DevTools {

		static bool devView = false;
		static GameObject spawnedGo = null;

		static bool displayClosestObjectNames = false;
		static bool airBreak = false;

		public static bool netStats = false;
		public static bool displayPlayerDebug = false;

		/// <summary>
		/// Game object representing local player.
		/// </summary>
		static GameObject localPlayer = null;

		public static void OnGUI() {
			if (displayClosestObjectNames) {
				DrawClosestObjectNames();
			}

			if (!devView) {
				return;
			}

			const float DEV_MENU_BUTTON_WIDTH = 150.0f;
			const float TITLE_SECTION_WIDTH = 50.0f;
			var devMenuButtonsRect = new Rect(5, 25.0f, DEV_MENU_BUTTON_WIDTH, 25.0f);

			// section title
			GUI.color = Color.white;
			GUI.Label(devMenuButtonsRect, "Toggles:");
			devMenuButtonsRect.x += TITLE_SECTION_WIDTH;

			// net stats
			GUI.color = netStats ? Color.green : Color.white;
			if (GUI.Button(devMenuButtonsRect, "Net stats")) {
				netStats = !netStats;
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;

			// player debug
			GUI.color = displayPlayerDebug ? Color.green : Color.white;
			if (GUI.Button(devMenuButtonsRect, "Net stats - players dbg")) {
				displayPlayerDebug = !displayPlayerDebug;
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;

			// displayClosestObjectNames

			GUI.color = displayClosestObjectNames ? Color.green : Color.white;
			if (GUI.Button(devMenuButtonsRect, "Display object names")) {
				displayClosestObjectNames = !displayClosestObjectNames;
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;

			// airbreak toggle

			GUI.color = airBreak ? Color.green : Color.white;
			if (GUI.Button(devMenuButtonsRect, "AirBreak")) {
				airBreak = !airBreak;
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;

			// START TOOLS
			devMenuButtonsRect.x = 5.0f;
			devMenuButtonsRect.y = 55.0f;

			GUI.color = Color.white;
			GUI.Label(devMenuButtonsRect, "Actions:");
			devMenuButtonsRect.x += TITLE_SECTION_WIDTH;

			// world dump

			GUI.color = Color.white;
			if (GUI.Button(devMenuButtonsRect, "Dump world")) {
				DumpWorld(Application.loadedLevelName);
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;

			GUI.color = Color.white;
			if (GUI.Button(devMenuButtonsRect, "Dump local player")) {
				DumpLocalPlayer();
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;
		}

		static void DrawClosestObjectNames() {
			foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
				if (localPlayer) {
					if ((go.transform.position - localPlayer.transform.position).sqrMagnitude > 10) {
						continue;
					}
				}

				Vector3 pos = Camera.main.WorldToScreenPoint(go.transform.position);
				if (pos.z < 0.0f) {
					continue;
				}


				GUI.Label(new Rect(pos.x, Screen.height - pos.y, 500, 20), go.name);
			}
		}

		public static void Update() {
			if (localPlayer == null) {
				localPlayer = GameObject.Find("PLAYER");
			}
			else {
				UpdatePlayer();
			}

			if (Input.GetKeyDown(KeyCode.F3)) {
				devView = !devView;
			}
		}

		public static void UpdatePlayer() {

			if (airBreak) {
				// Pseudo AirBrk
				if (Input.GetKey(KeyCode.KeypadPlus) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position + Vector3.up * 5.0f;
				}
				if (Input.GetKey(KeyCode.KeypadMinus) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position - Vector3.up * 5.0f;
				}
				if (Input.GetKey(KeyCode.Keypad8) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.forward * 5.0f;
				}
				if (Input.GetKey(KeyCode.Keypad2) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position - localPlayer.transform.rotation * Vector3.forward * 5.0f;
				}
				if (Input.GetKey(KeyCode.Keypad4) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position - localPlayer.transform.rotation * Vector3.right * 5.0f;
				}
				if (Input.GetKey(KeyCode.Keypad6) && localPlayer) {
					localPlayer.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.right * 5.0f;
				}
			}
		}

		static void DumpLocalPlayer() {
			StringBuilder builder = new StringBuilder();
			Utils.PrintTransformTree(localPlayer.transform, 0, (int level, string text) => {

				for (int i = 0; i < level; ++i) builder.Append("    ");
				builder.Append(text + "\n");
			});
			System.IO.File.WriteAllText(Client.GetPath("localPlayer.txt"), builder.ToString());
		}

		public static void DumpWorld(string levelName) {
			GameObject []gos = gos = GameObject.FindObjectsOfType<GameObject>();

			string path = Client.GetPath($"WorldDump\\{levelName}");
			Directory.CreateDirectory(path);

			StringBuilder builder = new StringBuilder();
			int index = 0;
			foreach (GameObject go in gos) {
				Transform trans = go.GetComponent<Transform>();
				if (trans == null || trans.parent != null) continue;

				string SanitizedName = go.name;
				SanitizedName = SanitizedName.Replace("/", "");
				string dumpFilePath = path + "\\" + SanitizedName + ".txt";
				try {
					DumpObject(trans, dumpFilePath);
				} catch (Exception e) {
					Logger.Log("Unable to dump objects: " + SanitizedName + "\n");
					Logger.Log(e.Message + "\n");
				}

				builder.Append(go.name + " (" + SanitizedName + "), Trans: " + trans.position.ToString() + "\n");
				++index;
			}

			System.IO.File.WriteAllText(path + "\\dumpLog.txt", builder.ToString());
		}

		public static void DumpObject(Transform obj, string file) {
			StringBuilder bldr = new StringBuilder();
			Utils.PrintTransformTree(obj, 0, (int level, string text) => {
				for (int i = 0; i < level; ++i) bldr.Append("    ");
				bldr.Append(text + "\n");
			});

			System.IO.File.WriteAllText(file, bldr.ToString());
		}
	}
#endif
}
