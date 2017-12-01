using UnityEngine;
using HutongGames.PlayMaker;

namespace MSCMP {
	/// <summary>
	/// Various utilities.
	/// </summary>
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
					print(level + 1, "Active state: " + pmfsm.ActiveStateName);

					FsmEvent[] events = pmfsm.FsmEvents;
					foreach (FsmEvent e in events) {
						print(level + 2, "Event Name: " + e.Name + " (" + e.Path + ")");
					}

					foreach (FsmTransition t in pmfsm.FsmGlobalTransitions) {
						print(level + 2, "Global transition: " + t.EventName + " > " + t.ToState);
					}



					FsmState[] states = pmfsm.FsmStates;
					foreach (FsmState s in states) {
						print(level + 2, "State Name: " + s.Name);
						foreach (FsmTransition t in s.Transitions) {
							print(level + 3, "Transition: " + t.EventName + " > " + t.ToState);
						}

						try {
							foreach (FsmStateAction a in s.ActionData.LoadActions(s)) {
								print(level + 3, "Action Name: " + a.Name);
							}
						}
						catch {
							MPController.logFile.Write("Failed to dump actions for state: " + s.Name);
						}
					}

					FsmVariables variables = pmfsm.FsmVariables;
					foreach (var v in variables.BoolVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.ColorVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.FloatVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.GameObjectVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.IntVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.MaterialVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.ObjectVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.QuaternionVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.RectVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.StringVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.TextureVariables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
					foreach (var v in variables.Vector2Variables) {
						print(level + 2, "  Variable Name: " + v.Name);
					}
					foreach (var v in variables.Vector3Variables) {
						print(level + 2, "Variable Name: " + v.Name);
					}
				}
				else if (component is Animation) {
					var anim = (Animation)component;
					foreach (AnimationState state in anim) {
						print(level + 1, "Animation state: " + state.name);
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
