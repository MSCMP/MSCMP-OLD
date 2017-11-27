using UnityEngine;
using System.Text;
using System;
using HutongGames.PlayMaker;

namespace MSCMP {
	class MPGameObject : MonoBehaviour {
		GameObject[] gos = null;

		Texture2D fillText = new Texture2D(1, 1);

		void Start() {
			DontDestroyOnLoad(this.gameObject);

			fillText.SetPixel(0, 0, Color.white);
			fillText.wrapMode = TextureWrapMode.Repeat;
			fillText.Apply();
		}

		PlayMakerFSM GetPlaymakerScriptByName(GameObject go, string name) {
			PlayMakerFSM[] fsms = go.GetComponents<PlayMakerFSM>();

			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.FsmName == name) {
					return fsm;
				}
			}
			fsms = go.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.FsmName == name) {
					return fsm;
				}
			}
			return null;
		}

		private Vector2 scrollViewVector = Vector2.zero;
		GameObject localPlayer = null;
		delegate void PrintInfo(int level, string data);
		void PrintTrans(Transform trans, int level, PrintInfo print) {
			print(level, "> "+ trans.name);

			Component[] components = trans.GetComponents<Component>();
			foreach (Component component in components) {
				print(level, " C " + component.GetType().FullName);

				if (component is PlayMakerFSM) {
					PlayMakerFSM pmfsm = (PlayMakerFSM)component;
					print(level + 1, "PMFSM Name: " + pmfsm.FsmName);

					FsmEvent[] events = pmfsm.FsmEvents;
					foreach (FsmEvent e in events) {
						print(level + 2, "  Event Name: " + e.Name);
					}

					FsmState[] states = pmfsm.FsmStates;
					foreach (FsmState s in states) {
						print(level + 2, "  State Name: " + s.Name);
					}

					FsmVariables variables = pmfsm.FsmVariables;
					foreach (var v in variables.BoolVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.ColorVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.FloatVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.GameObjectVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.IntVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.MaterialVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.ObjectVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.QuaternionVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.RectVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.StringVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.TextureVariables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.Vector2Variables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.Vector3Variables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
				}
			}

			for (int i = 0; i < trans.childCount; ++i) {
				Transform child = trans.GetChild(i);
				PrintTrans(child, level + 1, print);
			}
		}


		bool devView = false;
		GameObject spawnedGo = null;

		void OnGUI() {
			GUI.color = Color.red;
			GUI.Label(new Rect(1, 1, 500, 20), "MSCMP Development Build <3");




			foreach (GameObject go in gos) {

				if (localPlayer) {
					if ((go.transform.position - localPlayer.transform.position).sqrMagnitude > 10) {
						continue;
					}
				}

				if (go.transform.parent != null) {
					continue;
				}
				Vector3 pos = Camera.main.WorldToScreenPoint(go.transform.position);
				if (pos.z < 0.0f) {
					continue;
				}


				GUI.Label(new Rect(pos.x, Screen.height - pos.y, 500, 20), go.name);
			}

			if (spawnedGo) {
				Transform trans = spawnedGo.GetComponent<Transform>();
				string parentName = trans.parent != null ? trans.parent.name : "(no parent)";
				if (GetPlaymakerScriptByName(spawnedGo, "LOD")) {
					parentName += " has lod";
				}
				GUI.Label(new Rect(1, 50, 500, 20), "spawnedGo pos: " + trans.position.ToString() + " " + parentName);
			}

			if (localPlayer != null) {
				Transform trans = localPlayer.GetComponent<Transform>();

				GUI.Label(new Rect(1, 30, 500, 20), "Character pos: " + trans.position.ToString());

				if (devView) {

					GUI.backgroundColor = Color.red;
					scrollViewVector = GUI.BeginScrollView(new Rect(1, 40, 500, 300), scrollViewVector, new Rect(0, 0, 500, 7000));
					int index = 0;
					GUI.color = Color.white;
					PrintTrans(trans, 0, (int level, string text) => {
						GUI.Label(new Rect(level * 10, index * 18, 500, 20), text);
						index++;
					});
					GUI.EndScrollView();

					GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
					GUI.DrawTexture(new Rect(1, 40, 500, 300), fillText, ScaleMode.StretchToFill, true);
				}
			}
		}


		void Update() {


			if (Input.GetKeyDown(KeyCode.F3)) {
				devView = !devView;
			}

			gos = GameObject.FindObjectsOfType<GameObject>();

			if (localPlayer == null) {
				localPlayer = GameObject.Find("PLAYER");
			}

			// Pseudo AirBrk
			if (Input.GetKeyDown(KeyCode.KeypadPlus) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position + Vector3.up * 5.0f;
			}
			if (Input.GetKeyDown(KeyCode.KeypadMinus) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position - Vector3.up * 5.0f;
			}
			if (Input.GetKeyDown(KeyCode.Keypad8) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.forward * 5.0f;
			}
			if (Input.GetKeyDown(KeyCode.Keypad2) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position - localPlayer.transform.rotation * Vector3.forward * 5.0f;
			}
			if (Input.GetKeyDown(KeyCode.Keypad4) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position - localPlayer.transform.rotation * Vector3.right * 5.0f;
			}
			if (Input.GetKeyDown(KeyCode.Keypad6) && localPlayer) {
				localPlayer.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.right * 5.0f;
			}

			if (Input.GetKeyDown(KeyCode.G) && localPlayer) {
				PlayMakerFSM fsm = GetPlaymakerScriptByName(localPlayer, "PlayerFunctions");
				if (fsm != null) {
					fsm.SendEvent("MIDDLEFINGER");
				}
				else {
					GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
					go.transform.position = localPlayer.transform.position + localPlayer.transform.rotation * Vector3.forward * 2.0f;
				}
			}

			if (Input.GetKeyDown(KeyCode.I) && localPlayer) {

				StringBuilder builder = new StringBuilder();
				PrintTrans(localPlayer.transform, 0, (int level, string text) => {

					for (int i = 0; i < level; ++i) builder.Append("    ");
					builder.Append(text + "\n");
				});
				System.IO.File.WriteAllText("J:\\projects\\MSCMP\\MSCMP\\Debug\\localPlayer.txt", builder.ToString());
			}


			if (Input.GetKeyDown(KeyCode.F6) && localPlayer) {




				GameObject prefab = GameObject.Find("JONNEZ ES(Clone)");
				spawnedGo = Instantiate(prefab);

				// Remove component that overrides spawn position of JONNEZ.
				PlayMakerFSM fsm = GetPlaymakerScriptByName(spawnedGo, "LOD");
				Destroy(fsm);

				Vector3 direction = localPlayer.transform.rotation * Vector3.forward * 2.0f;
				spawnedGo.transform.position = localPlayer.transform.position + direction;


				/*StringBuilder builder = new StringBuilder();
				PrintTrans(go.transform, 0, (int level, string text) => {

					for (int i = 0; i < level; ++i)	builder.Append("    ");
					builder.Append(text + "\n");
				});
				System.IO.File.WriteAllText("J:\\projects\\MSCMP\\MSCMP\\Debug\\charTree.txt", builder.ToString());*/


			}

			if (Input.GetKeyDown(KeyCode.F5) && gos != null) {


				GUI.color = Color.white;
				int index = 0;

				StringBuilder builder = new StringBuilder();
				foreach (GameObject go in gos) {
					Transform trans = go.GetComponent<Transform>();
					if (trans == null || trans.parent != null) continue;


					StringBuilder bldr = new StringBuilder();
					PrintTrans(trans, 0, (int level, string text) => {

						for (int i = 0; i < level; ++i) builder.Append("    ");
						bldr.Append(text + "\n");
					});
					System.IO.File.WriteAllText("J:\\projects\\MSCMP\\MSCMP\\Debug\\WorldDump\\" + go.name + ".txt", bldr.ToString());

					builder.Append(go.name + ", Trans: " + trans.position.ToString() + "\n");
					++index;
				}

				System.IO.File.WriteAllText("J:\\projects\\MSCMP\\MSCMP\\Debug\\gos.txt", builder.ToString());
			}
		}
	}
}
