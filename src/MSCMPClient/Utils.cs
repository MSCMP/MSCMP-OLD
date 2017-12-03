using UnityEngine;
using HutongGames.PlayMaker;
using System;
using System.Reflection;

namespace MSCMP {
	/// <summary>
	/// Various utilities.
	/// </summary>
	class Utils {

		public delegate void PrintInfo(int level, string data);

		private static void PrintPlayMakerActionDetails(int level, FsmStateAction rawAction, PrintInfo print) {
			/*if (rawAction is SendEventByName) {
				var action = (SendEventByName)rawAction;

				print(level, "delay = " + action.delay.Value);
				print(level, "eventTarget = " + ((action.eventTarget != null) ? action.eventTarget.fsmName.Value : "null"));
				print(level, "everyFrame = " + action.everyFrame);
				print(level, "sendEvent = " + action.sendEvent.Value);
			}*/

			Type type = rawAction.GetType();

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var fi in fields) {
				print(level, fi.Name + " = " + fi.GetValue(rawAction).ToString());
			}
		}

		public static void PrintTransformTree(Transform trans, int level, PrintInfo print) {
			print(level, "> " + trans.name + " (" + trans.GetInstanceID() + ")");

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
							foreach (FsmStateAction a in s.Actions) {
								print(level + 3, "Action Name: " + a.Name + " (" + a.GetType().FullName + ")");
								PrintPlayMakerActionDetails(level + 4, a, print);
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


		public static Network.Messages.Vector3Message GameVec3ToNet(Vector3 v3) {
			var msg = new Network.Messages.Vector3Message();
			msg.x = v3.x;
			msg.y = v3.y;
			msg.z = v3.z;
			return msg;
		}


		public static Vector3 NetVec3ToGame(Network.Messages.Vector3Message msg) {
			var vec = new Vector3();
			vec.x = msg.x;
			vec.y = msg.y;
			vec.z = msg.z;
			return vec;
		}

		public static Network.Messages.QuaternionMessage GameQuatToNet(Quaternion q) {
			var msg = new Network.Messages.QuaternionMessage();
			msg.w = q.w;
			msg.x = q.x;
			msg.y = q.y;
			msg.z = q.z;
			return msg;
		}


		public static Quaternion NetQuatToGame(Network.Messages.QuaternionMessage msg) {
			var q = new Quaternion();
			q.w = msg.w;
			q.x = msg.x;
			q.y = msg.y;
			q.z = msg.z;
			return q;
		}
	}
}
