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
				GameCallbacks.onPlayMakerObjectCreate
						?.Invoke(storeObject.Value, gameObject.Value);
			}
		}

		/// <summary>
		/// Destroy object PlayMaker action hook.
		/// </summary>
		class MyDestroyObject : DestroyObject {
			public override void OnEnter() {
				if (gameObject.Value != null) {
					GameCallbacks.onPlayMakerObjectDestroy?.Invoke(gameObject.Value);
				}
				base.OnEnter();
			}
		}

		/// <summary>
		/// Destroy self PlayMaker action hook.
		/// </summary>
		class MyDestroySelf : DestroySelf {
			public override void OnEnter() {
				GameCallbacks.onPlayMakerObjectDestroy?.Invoke(Owner);
				base.OnEnter();
			}
		}

		/// <summary>
		/// Activate game object PlayMaker action hook.
		/// </summary>
		class MyActivateGameObject : ActivateGameObject {
			public override void OnEnter() {
				UnityEngine.GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
				if (go == null) {
					Finish();
					return;
				}
				GameCallbacks.onPlayMakerObjectActivate?.Invoke(go, activate.Value);
				base.OnEnter();
			}
		}

		/// <summary>
		/// Set game object position PlayMaker action hook.
		/// </summary>
		class MySetPosition : SetPosition {
			public override void OnEnter() {
				UnityEngine.GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
				if (go == null) {
					Finish();
					return;
				}

				UnityEngine.Vector3 newPosition = this.vector.Value;
				if (!this.x.IsNone) newPosition.x = this.x.Value;
				if (!this.y.IsNone) newPosition.y = this.y.Value;
				if (!this.z.IsNone) newPosition.z = this.z.Value;

				GameCallbacks.onPlayMakerSetPosition?.Invoke(go, newPosition, space);

				base.OnEnter();
			}
		}

		// Start ES2 actions hook - disable saving and loading when Player.

		/// <summary>
		/// Is this file whitelisted and can be saved even for player (not for host)?
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>true if file is whitelisted, false otherwise</returns>
		static bool IsWhitelistedFile(string fileName) {
			return fileName == "options.txt";
		}

		private class MySaveAudioClip : SaveAudioClip {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveBool : SaveBool {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveBoxCollider : SaveBoxCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveCapsuleCollider : SaveCapsuleCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveColor : SaveColor {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveFloat : SaveFloat {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveInt : SaveInt {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveMaterial : SaveMaterial {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveMeshCollider : SaveMeshCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveQuaternion : SaveQuaternion {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveSphereCollider : SaveSphereCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveString : SaveString {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveTexture : SaveTexture {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveTransform : SaveTransform {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveVector2 : SaveVector2 {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		private class MySaveVector3 : SaveVector3 {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}

		public class MyLoadAudioClip : LoadAudioClip {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadBool : LoadBool {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadBoxCollider : LoadBoxCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadCapsuleCollider : LoadCapsuleCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadColor : LoadColor {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadFloat : LoadFloat {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadInt : LoadInt {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadMaterial : LoadMaterial {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadMeshCollider : LoadMeshCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadQuaternion : LoadQuaternion {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadSphereCollider : LoadSphereCollider {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadString : LoadString {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadTexture : LoadTexture {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadTransform : LoadTransform {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadVector2 : LoadVector2 {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}
		public class MyLoadVector3 : LoadVector3 {
			public override void OnEnter() {
				if (MPController.Instance.CanUseSave || IsWhitelistedFile(saveFile.Value)) {
					base.OnEnter();
				} else {
					Finish();
				}
			}
		}

		// End ES2 actions hook - disable saving and loading when Player.

		/// <summary>
		/// Install PlayMaker actions hooks.
		/// </summary>
		public static void Install() {
			Utils.CallSafe("Hook PlayMaker actions", () => {
				Type type = typeof(ActionData);
				FieldInfo actionTypeLookup = type.GetField(
						"ActionTypeLookup", BindingFlags.Static | BindingFlags.NonPublic);

				Dictionary<string, System.Type> value =
						(Dictionary<string, System.Type>)actionTypeLookup.GetValue(null);
				value.Add(
						"HutongGames.PlayMaker.Actions.CreateObject", typeof(MyCreateObject));
				value.Add(
						"HutongGames.PlayMaker.Actions.DestroySelf", typeof(MyDestroySelf));
				value.Add(
						"HutongGames.PlayMaker.Actions.DestroyObject", typeof(MyDestroyObject));
				value.Add("HutongGames.PlayMaker.Actions.ActivateGameObject",
						typeof(MyActivateGameObject));
				value.Add(
						"HutongGames.PlayMaker.Actions.SetPosition", typeof(MySetPosition));

				// ES2 actions hooks

				value.Add(
						"HutongGames.PlayMaker.Actions.SaveAudioClip", typeof(MySaveAudioClip));
				value.Add("HutongGames.PlayMaker.Actions.SaveBool", typeof(MySaveBool));
				value.Add("HutongGames.PlayMaker.Actions.SaveBoxCollider",
						typeof(MySaveBoxCollider));
				value.Add("HutongGames.PlayMaker.Actions.SaveCapsuleCollider",
						typeof(MySaveCapsuleCollider));
				value.Add("HutongGames.PlayMaker.Actions.SaveColor", typeof(MySaveColor));
				value.Add("HutongGames.PlayMaker.Actions.SaveFloat", typeof(MySaveFloat));
				value.Add("HutongGames.PlayMaker.Actions.SaveInt", typeof(MySaveInt));
				value.Add(
						"HutongGames.PlayMaker.Actions.SaveMaterial", typeof(MySaveMaterial));
				value.Add("HutongGames.PlayMaker.Actions.SaveMeshCollider",
						typeof(MySaveMeshCollider));
				value.Add("HutongGames.PlayMaker.Actions.SaveQuaternion",
						typeof(MySaveQuaternion));
				value.Add("HutongGames.PlayMaker.Actions.SaveSphereCollider",
						typeof(MySaveSphereCollider));
				value.Add("HutongGames.PlayMaker.Actions.SaveString", typeof(MySaveString));
				value.Add(
						"HutongGames.PlayMaker.Actions.SaveTexture", typeof(MySaveTexture));
				value.Add(
						"HutongGames.PlayMaker.Actions.SaveTransform", typeof(MySaveTransform));
				value.Add(
						"HutongGames.PlayMaker.Actions.SaveVector2", typeof(MySaveVector2));
				value.Add(
						"HutongGames.PlayMaker.Actions.SaveVector3", typeof(MySaveVector3));

				value.Add(
						"HutongGames.PlayMaker.Actions.LoadAudioClip", typeof(MyLoadAudioClip));
				value.Add("HutongGames.PlayMaker.Actions.LoadBool", typeof(MyLoadBool));
				value.Add("HutongGames.PlayMaker.Actions.LoadBoxCollider",
						typeof(MyLoadBoxCollider));
				value.Add("HutongGames.PlayMaker.Actions.LoadCapsuleCollider",
						typeof(MyLoadCapsuleCollider));
				value.Add("HutongGames.PlayMaker.Actions.LoadColor", typeof(MyLoadColor));
				value.Add("HutongGames.PlayMaker.Actions.LoadFloat", typeof(MyLoadFloat));
				value.Add("HutongGames.PlayMaker.Actions.LoadInt", typeof(MyLoadInt));
				value.Add(
						"HutongGames.PlayMaker.Actions.LoadMaterial", typeof(MyLoadMaterial));
				value.Add("HutongGames.PlayMaker.Actions.LoadMeshCollider",
						typeof(MyLoadMeshCollider));
				value.Add("HutongGames.PlayMaker.Actions.LoadQuaternion",
						typeof(MyLoadQuaternion));
				value.Add("HutongGames.PlayMaker.Actions.LoadSphereCollider",
						typeof(MyLoadSphereCollider));
				value.Add("HutongGames.PlayMaker.Actions.LoadString", typeof(MyLoadString));
				value.Add(
						"HutongGames.PlayMaker.Actions.LoadTexture", typeof(MyLoadTexture));
				value.Add(
						"HutongGames.PlayMaker.Actions.LoadTransform", typeof(MyLoadTransform));
				value.Add(
						"HutongGames.PlayMaker.Actions.LoadVector2", typeof(MyLoadVector2));
				value.Add(
						"HutongGames.PlayMaker.Actions.LoadVector3", typeof(MyLoadVector3));
			});
		}
	}
}
