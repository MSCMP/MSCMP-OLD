using UnityEngine;
using HutongGames.PlayMaker;

namespace MSCMP {
	class Utils {

		public delegate void PrintInfo(int level, string data);

		public static void PrintTransformTree(Transform trans, int level, PrintInfo print) {
			print(level, "> " + trans.name);

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
				PrintTransformTree(child, level + 1, print);
			}
		}

		public static PlayMakerFSM GetPlaymakerScriptByName(GameObject go, string name) {
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

	}
}
