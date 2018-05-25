using UnityEngine;
using System.Text;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSCMP {
#if !PUBLIC_RELEASE
	/// <summary>
	/// Development tools.
	/// </summary>
	static class DevTools {

		static bool devView = false;

		static bool displayClosestObjectNames = false;
		static bool airBreak = false;

		public static bool netStats = false;
		public static bool displayPlayerDebug = false;

		/// <summary>
		/// Game object representing local player.
		/// </summary>
		static GameObject localPlayer = null;

		const float DEV_MENU_BUTTON_WIDTH = 150.0f;
		const float TITLE_SECTION_WIDTH = 50.0f;
		static Rect devMenuButtonsRect = new Rect(5, 0.0f, DEV_MENU_BUTTON_WIDTH, 25.0f);

		public static void OnInit() {
			//Teleports a game object to you
			UI.Console.RegisterCommand("gethere", (string[] args) => {
				if (args.Length == 1) {
					Client.ConsoleMessage($"ERROR: Invalid syntax. Use 'gethere [gameObjectName]'.");
					return;
				}

				if (localPlayer == null) { Client.ConsoleMessage("ERROR: Couldn't find local player."); return; }

				string ourObjectName = String.Join(" ", args.Skip(1).ToArray());
				GameObject ourObject = GameObject.Find(ourObjectName);
				if (ourObject == null) {
					Client.ConsoleMessage($"ERROR: Couldn't find {ourObjectName}.");
					return;
				}

				ourObject.transform.rotation = localPlayer.transform.rotation;
				ourObject.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.forward * 5.0f;
				Client.ConsoleMessage($"Teleported {ourObjectName} to you!");
			});

			//Teleports yourself to a game object
			UI.Console.RegisterCommand("goto", (string[] args) => {
				if (args.Length == 1) { Client.ConsoleMessage($"ERROR: Invalid syntax. Use 'goto [gameObjectName]'."); return; }

				if (localPlayer == null) { Client.ConsoleMessage("ERROR: Couldn't find local player."); return; }

				string ourObjectName = String.Join(" ", args.Skip(1).ToArray());
				GameObject ourObject = GameObject.Find(ourObjectName);
				if (ourObject == null) { Client.ConsoleMessage($"ERROR: Couldn't find {ourObjectName}."); return; }

				localPlayer.transform.position = ourObject.transform.position + Vector3.up * 2.0f;
				Client.ConsoleMessage($"Teleported to {ourObjectName} !");
			});
		}

		public static void OnGUI() {
			if (displayClosestObjectNames) {
				DrawClosestObjectNames();
			}

			if (!devView) {
				return;
			}


			devMenuButtonsRect.x = 5.0f;
			devMenuButtonsRect.y = 0.0f;

			NewSection("Toggles:");
			Checkbox("Net stats", ref netStats);
			Checkbox("Net stats - players dbg", ref displayPlayerDebug);
			Checkbox("Display object names", ref displayClosestObjectNames);
			Checkbox("AirBreak", ref airBreak);

			NewSection("Actions:");

			if (Action("Dump world")) {
				DumpWorld(Application.loadedLevelName);
			}

			if (Action("Dump local player")) {
				DumpLocalPlayer();
			}
		}

		static void NewSection(string title) {
			devMenuButtonsRect.x = 5.0f;
			devMenuButtonsRect.y += 25.0f;

			GUI.color = Color.white;
			GUI.Label(devMenuButtonsRect, title);
			devMenuButtonsRect.x += TITLE_SECTION_WIDTH;
		}

		static void Checkbox(string name, ref bool state) {
			GUI.color = state ? Color.green : Color.white;
			if (GUI.Button(devMenuButtonsRect, name)) {
				state = !state;
			}
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;
		}

		static bool Action(string name) {
			GUI.color = Color.white;
			bool execute = GUI.Button(devMenuButtonsRect, name);
			devMenuButtonsRect.x += DEV_MENU_BUTTON_WIDTH;
			return execute;
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
			Utils.CallSafe("DUmpWorld", ()=> {
				Development.WorldDumper worldDumper = new Development.WorldDumper();
				string dumpFolder = Client.GetPath($"HTMLWorldDump\\{levelName}");
				Directory.Delete(dumpFolder, true);
				Directory.CreateDirectory(dumpFolder);

				var watch = Stopwatch.StartNew();

				worldDumper.Dump(dumpFolder);

				Logger.Log($"World dump finished - took {watch.ElapsedMilliseconds} ms");
			});

			/*GameObject[] gos = gos = GameObject.FindObjectsOfType<GameObject>();

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
				}
				catch (Exception e) {
					Logger.Log("Unable to dump objects: " + SanitizedName + "\n");
					Logger.Log(e.Message + "\n");
				}

				builder.Append(go.name + " (" + SanitizedName + "), Trans: " + trans.position.ToString() + "\n");
				++index;
			}

			System.IO.File.WriteAllText(path + "\\dumpLog.txt", builder.ToString());*/
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