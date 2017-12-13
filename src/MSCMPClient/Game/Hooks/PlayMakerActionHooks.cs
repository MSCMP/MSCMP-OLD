using System;
using System.Reflection;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace MSCMP.Game.Hooks {
	/// <summary>
	/// Class containing various PlayMaker action hooks.
	/// </summary>
	static class PlayMakerActionHooks {

		/// <summary>
		/// Create object PlayMaker action hook.
		/// </summary>
		class MyCreateObject : CreateObject {
			public override void OnEnter() {
				base.OnEnter();
				GameCallbacks.onPlayMakerObjectCreate?.Invoke(storeObject.Value, gameObject.Value);
			}
		}

		/// <summary>
		/// Destroy object PlayMaker action hook.
		/// </summary>
		class MyDestroyObject : DestroyObject {
			public override void OnEnter() {
				GameCallbacks.onPlayMakerObjectDestroy?.Invoke(gameObject.Value);
				base.OnEnter();
			}
		}

		/// <summary>
		/// Install PlayMaker actions hooks.
		/// </summary>
		public static void Install() {
			Utils.CallSafe("Hook PlayMaker actions", () => {
				Type type = typeof(ActionData);
				FieldInfo actionTypeLookup = type.GetField("ActionTypeLookup", BindingFlags.Static | BindingFlags.NonPublic);

				Dictionary<string, System.Type> value = (Dictionary<string, System.Type>)actionTypeLookup.GetValue(null);
				value.Add("HutongGames.PlayMaker.Actions.CreateObject", typeof(MyCreateObject));
				value.Add("HutongGames.PlayMaker.Actions.DestroyObject", typeof(MyDestroyObject));
			});
		}
	}
}
