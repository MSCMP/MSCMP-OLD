using UnityEngine;
using HutongGames.PlayMaker;
using System;
using System.Reflection;

namespace MSCMP {
	/// <summary>
	/// Various utilities.
	/// </summary>
	class Utils {

		public const int LAYER_DEFAULT = 1 << 0;
		public const int LAYER_TRANSPARENT_FX = 1 << 1;
		public const int LAYER_IGNORE_RAYCAST = 1 << 2;
		public const int LAYER_WATER = 1 << 4;
		public const int LAYER_UI = 1 << 5;

		/// <summary>
		/// Delegate used to print tree of the objects.
		/// </summary>
		/// <param name="level">The level - can be used to generate identation.</param>
		/// <param name="data">The line data.</param>
		public delegate void PrintInfo(int level, string data);

		/// <summary>
		/// Print details about play maker action.
		/// </summary>
		/// <param name="level">The level of the print.</param>
		/// <param name="rawAction">The base typed object contaning action.</param>
		/// <param name="print">The delegate to call to print value.</param>
		private static void PrintPlayMakerActionDetails(int level, FsmStateAction rawAction, PrintInfo print) {
			Type type = rawAction.GetType();

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var fi in fields) {
				print(level, fi.Name + " = " + fi.GetValue(rawAction).ToString());
			}
		}

		/// <summary>
		/// Prints unity Transform tree starting from trans.
		/// </summary>
		/// <param name="trans">The transform object to start print of the tree.</param>
		/// <param name="level">The level of print. When starting printing it should be 0.</param>
		/// <param name="print">The delegate to call to print value.</param>
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
							Logger.Log("Failed to dump actions for state: " + s.Name);
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

		/// <summary>
		/// Get PlayMaker finite-state-matching from the game objects tree starting from game object.
		/// </summary>
		/// <param name="go">The game object to start searching at.</param>
		/// <param name="name">The name of finite-state-machine to find.</param>
		/// <returns>Finite state machine matching the name or null if no such state machine is found.</returns>
		public static PlayMakerFSM GetPlaymakerScriptByName(GameObject go, string name) {
			PlayMakerFSM[] fsms = go.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.FsmName == name) {
					return fsm;
				}
			}
			return null;
		}

		/// <summary>
		/// Convert game representation of vector into network message.
		/// </summary>
		/// <param name="v3">Vector to convert.</param>
		/// <returns>Vector network message.</returns>
		public static Network.Messages.Vector3Message GameVec3ToNet(Vector3 v3) {
			var msg = new Network.Messages.Vector3Message();
			msg.x = v3.x;
			msg.y = v3.y;
			msg.z = v3.z;
			return msg;
		}

		/// <summary>
		/// Convert network message containing vector into game representation of vector.
		/// </summary>
		/// <param name="msg">The message to convert.</param>
		/// <returns>Converted vector.</returns>
		public static Vector3 NetVec3ToGame(Network.Messages.Vector3Message msg) {
			var vec = new Vector3();
			vec.x = msg.x;
			vec.y = msg.y;
			vec.z = msg.z;
			return vec;
		}

		/// <summary>
		/// Convert game representation of quaternion into network message.
		/// </summary>
		/// <param name="q">Quaternion to convert.</param>
		/// <returns>Quaternion network message.</returns>
		public static Network.Messages.QuaternionMessage GameQuatToNet(Quaternion q) {
			var msg = new Network.Messages.QuaternionMessage();
			msg.w = q.w;
			msg.x = q.x;
			msg.y = q.y;
			msg.z = q.z;
			return msg;
		}

		/// <summary>
		/// Convert network message containing quaternion into game representation of quaternion.
		/// </summary>
		/// <param name="msg">The message to convert.</param>
		/// <returns>Converted quaternion.</returns>
		public static Quaternion NetQuatToGame(Network.Messages.QuaternionMessage msg) {
			var q = new Quaternion();
			q.w = msg.w;
			q.x = msg.x;
			q.y = msg.y;
			q.z = msg.z;
			return q;
		}

		/// <summary>
		/// Delegate contaning safe call code.
		/// </summary>
		public delegate void SafeCall();

		/// <summary>
		/// Perform safe call catching all exceptions that could happen within it's scope.
		/// </summary>
		/// <param name="name">The name of the safe call scope.</param>
		/// <param name="call">The code to execute.</param>
		public static void CallSafe(string name, SafeCall call) {
			try {
				call();
			}
			catch (Exception e) {
				Client.FatalError("Safe call " + name + " failed.\n" + e.Message + "\n" + e.StackTrace);
			}
		}
	}
}
