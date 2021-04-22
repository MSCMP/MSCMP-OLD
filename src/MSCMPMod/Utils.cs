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
		/// Print details about the given object.
		/// </summary>
		/// <param name="level">The level of the print.</param>
		/// <param name="obj">The base typed object contaning action.</param>
		/// <param name="print">The delegate to call to print value.</param>
		private static void PrintObjectFields(int level, object obj, PrintInfo print) {
			if (obj == null) { return; }

			if (level > 10) {
				print(level + 1, "Out of depth limit.");
				return;
			}

			Type type = obj.GetType();
			FieldInfo[] fields =
					type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
							BindingFlags.Public | BindingFlags.FlattenHierarchy);

			foreach (var fi in fields) {
				var val = fi.GetValue(obj);
				var fieldType = fi.FieldType;

				if (val == null) {
					print(level, fieldType.FullName + " " + fi.Name + " = null");
					continue;
				}

				string additionalString = "";

				if (val is NamedVariable) {
					additionalString += $" [Named variable: {((NamedVariable)val).Name}]";
				}

				print(level,
						fieldType.FullName + " " + fi.Name + " = " + val.ToString() +
								additionalString);

				if (fieldType.IsClass &&
						(fieldType.Namespace == null ||
								!fieldType.Namespace.StartsWith("System"))) {
					PrintObjectFields(level + 1, val, print);
				}
			}
		}

		/// <summary>
		/// Helper getting named variable valeu as string.
		/// </summary>
		/// <param name="var"></param>
		/// <returns></returns>
		private static string GetNamedVariableValueAsString(NamedVariable var) {
			if (var == null) return "null";

			object value = null;
			if (var is FsmBool) { value = ((FsmBool)var).Value; }
			if (var is FsmColor) { value = ((FsmColor)var).Value; }
			if (var is FsmFloat) { value = ((FsmFloat)var).Value; }
			if (var is FsmGameObject) { value = ((FsmGameObject)var).Value; }
			if (var is FsmInt) { value = ((FsmInt)var).Value; }
			if (var is FsmMaterial) { value = ((FsmMaterial)var).Value; }
			if (var is FsmObject) { value = ((FsmObject)var).Value; }
			if (var is FsmQuaternion) { value = ((FsmQuaternion)var).Value; }
			if (var is FsmRect) { value = ((FsmRect)var).Value; }
			if (var is FsmString) { value = ((FsmString)var).Value; }
			if (var is FsmTexture) { value = ((FsmTexture)var).Value; }
			if (var is FsmVector2) { value = ((FsmVector2)var).Value; }
			if (var is FsmVector3) { value = ((FsmVector3)var).Value; }

			if (value == null) { return "null"; }
			return value.ToString();
		}

		/// <summary>
		/// Prints play maker fsm component details.
		/// </summary>
		/// <param name="pmfsm">The component to print detals for.</param>
		/// <param name="level">The level of print.</param>
		/// <param name="print">The method used to print the details.</param>
		private static void PrintPlaymakerFsmComponent(
				PlayMakerFSM pmfsm, int level, PrintInfo print) {
			// Make sure FSM is initialized.
			pmfsm.Fsm.Init(pmfsm);

			print(level, $"PMFSM Name: {pmfsm.FsmName}");
			print(level, $"Active state: {pmfsm.ActiveStateName}");
			print(level, $"Initialized: {pmfsm.Fsm.Initialized}");

			Logger.Log("EVENTS");
			FsmEvent[] events = pmfsm.FsmEvents;
			foreach (FsmEvent e in events) {
				if (e == null) {
					print(level, "Null event!");
					continue;
				}
				print(level, $"Event Name: {e.Name} ({e.Path})");
			}
			Logger.Log("GT");
			foreach (FsmTransition t in pmfsm.FsmGlobalTransitions) {
				if (t == null) {
					print(level, "Null global transition!");
					continue;
				}
				print(level, "Global transition: " + t.EventName + " > " + t.ToState);
			}
			Logger.Log("STATES");
			FsmState[] states = pmfsm.FsmStates;
			foreach (FsmState s in states) {
				if (s == null) {
					print(level, "Null state!");
					continue;
				}
				Logger.Log("PRE TRANS");

				print(level, $"State Name: {s.Name} (fsm: {s.Fsm}, go: {s.Fsm.GameObject})");
				foreach (FsmTransition t in s.Transitions) {
					if (t == null) {
						print(level + 1, "Null transition!");
						continue;
					}

					print(level + 1, "Transition: " + t.EventName + " > " + t.ToState);
				}
				Logger.Log("POST TRANS");
				Logger.Log("PRE ACTIONS");
				foreach (FsmStateAction a in s.Actions) {
					if (a == null) {
						print(level + 1, "Null action!");
						continue;
					}

					print(level + 1,
							"Action Name: " + a.Name + " (" + a.GetType().FullName + ")");
					PrintObjectFields(level + 2, a, print);
				}
				Logger.Log("POST ACTIONS");
			}
			Logger.Log("VARIABLES");
			print(level, "Variables:");
			NamedVariable[] variables = pmfsm.FsmVariables.GetAllNamedVariables();
			foreach (NamedVariable var in variables) {
				print(level + 1, $"{var.Name} = {GetNamedVariableValueAsString(var)}");
			}
		}

		/// <summary>
		/// Prints unity Transform components.
		/// </summary>
		/// <param name="trans">The transform object to print components of.</param>
		/// <param name="level">The level of print.</param>
		/// <param name="print">The delegate to call to print value.</param>
		private static void PrintTransformComponents(
				Transform trans, int level, PrintInfo print) {
			Component[] components = trans.GetComponents<Component>();
			foreach (Component component in components) {
				print(level + 1,
						"C " + component.GetType().FullName + " [" + component.tag + "]");

				if (component is PlayMakerFSM) {
					try {
						PrintPlaymakerFsmComponent((PlayMakerFSM)component, level + 2, print);
					} catch (Exception e) {
						Logger.Log("XXX");
						Logger.Log(e.StackTrace);
					}
				} else if (component is Animation) {
					var anim = (Animation)component;
					foreach (AnimationState state in anim) {
						print(level + 2, "Animation state: " + state.name);
					}
				}
			}
		}

		/// <summary>
		/// Prints unity Transform children.
		/// </summary>
		/// <param name="trans">The transform object to print children of.</param>
		/// <param name="level">The level of print.</param>
		/// <param name="print">The delegate to call to print value.</param>
		private static void PrintTransformChildren(
				Transform trans, int level, PrintInfo print) {
			for (int i = 0; i < trans.childCount; ++i) {
				Transform child = trans.GetChild(i);
				PrintTransformTree(child, level + 1, print);
			}
		}

		/// <summary>
		/// Prints unity Transform tree starting from trans.
		/// </summary>
		/// <param name="trans">The transform object to start print of the tree.</param>
		/// <param name="level">The level of print. When starting printing it should be
		/// 0.</param> <param name="print">The delegate to call to print value.</param>
		public static void PrintTransformTree(
				Transform trans, int level, PrintInfo print) {
			if (trans == null) { return; }

			print(level,
					$"> {trans.name} [{trans.tag}, {(trans.gameObject.activeSelf ? "active" : "inactive")}, {trans.gameObject.GetInstanceID()}]");

			PrintTransformComponents(trans, level, print);
			PrintTransformChildren(trans, level, print);
		}

		/// <summary>
		/// Get PlayMaker finite-state-matching from the game objects tree starting from
		/// game object.
		/// </summary>
		/// <param name="go">The game object to start searching at.</param>
		/// <param name="name">The name of finite-state-machine to find.</param>
		/// <returns>Finite state machine matching the name or null if no such state
		/// machine is found.</returns>
		public static PlayMakerFSM GetPlaymakerScriptByName(GameObject go, string name) {
			PlayMakerFSM[] fsms = go.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.FsmName == name) { return fsm; }
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
		/// Convert network message containing quaternion into game representation of
		/// quaternion.
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
		/// Perform safe call catching all exceptions that could happen within it's
		/// scope.
		/// </summary>
		/// <param name="name">The name of the safe call scope.</param>
		/// <param name="call">The code to execute.</param>
		public static void CallSafe(string name, SafeCall call) {
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached) {
				call();
				return;
			}
#endif

			try {
				call();
			} catch (Exception e) {
				Client.FatalError(
						"Safe call " + name + " failed.\n" + e.Message + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// Calculate jenkins hash of the given string.
		/// </summary>
		/// <param name="str">The string to calculate jenkins hash of.</param>
		/// <returns>The jenkins hash of the given string.</returns>
		public static int StringJenkinsHash(string str) {
			int i = 0;
			int hash = 0;
			while (i != str.Length) {
				hash += str[i++];
				hash += hash << 10;
				hash ^= hash >> 6;
			}
			hash += hash << 3;
			hash ^= hash >> 11;
			hash += hash << 15;
			return hash;
		}

		/// <summary>
		/// Check if hierarchy of the given game object matches.
		/// </summary>
		/// <param name="obj">The game object.</param>
		/// <param name="hierarchy">The hierarchy pattern to check.</param>
		/// <returns>true if hierarchy is matching, false otherwise</returns>
		public static bool IsGameObjectHierarchyMatching(
				GameObject obj, string hierarchy) {
			Transform current = obj.transform;
			var names = hierarchy.Split('/');
			for (int i = names.Length; i > 0; --i) {
				if (current == null) { return false; }

				if (names[i - 1] == "*") { continue; }

				if (current.name != names[i - 1]) { return false; }

				current = current.parent;
			}
			return true;
		}

		/// <summary>
		/// Convert p2p session error to string.
		/// </summary>
		/// <param name="sessionError">The session error.</param>
		/// <returns>Session error string.</returns>
		public static string P2PSessionErrorToString(
				Steamworks.EP2PSessionError sessionError) {
			switch (sessionError) {
			case Steamworks.EP2PSessionError.k_EP2PSessionErrorNone: return "none";
			case Steamworks.EP2PSessionError.k_EP2PSessionErrorNotRunningApp:
				return "not running app";
			case Steamworks.EP2PSessionError.k_EP2PSessionErrorNoRightsToApp:
				return "no rights to app";
			case Steamworks.EP2PSessionError.k_EP2PSessionErrorDestinationNotLoggedIn:
				return "user not logged in";
			case Steamworks.EP2PSessionError.k_EP2PSessionErrorTimeout: return "timeout";
			default: return "unknown";
			}
		}
	}
}
