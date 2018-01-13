using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using System;

namespace MSCMP
{
	/// <summary>
	/// Main class of the mod.
	/// </summary>
	public class Client {

		/// <summary>
		/// Asset bundle containing multiplayer mod content.
		/// </summary>
		static AssetBundle assetBundle = null;

		/// <summary>
		/// Starts the mod. Called from Injector.
		/// </summary>
		public static void Start() {
			Logger.SetAutoFlush(true);
			Game.Hooks.PlayMakerActionHooks.Install();

			string assetBundlePath = Client.GetPath("../../data/mpdata");
			if (!File.Exists(assetBundlePath)) {
				FatalError("Cannot find mpdata asset bundle.");
				return;
			}

			assetBundle = AssetBundle.CreateFromFile(assetBundlePath);

			var go = new GameObject("Multiplayer GUI Controller");
			go.AddComponent<UI.MPGUI>();

			go = new GameObject("Multiplayer Controller");
			go.AddComponent<MPController>();
		}

		/// <summary>
		/// Gets absolute path for the specified file relative to mod installation folder.
		/// </summary>
		/// <param name="file">The file to get path for.</param>
		/// <returns>Absolute path for the specified file relative to mod instalation folder.</returns>
		public static string GetPath(string file) {
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + file;
		}

		/// <summary>
		/// Loads asset from multiplayer mod asset bundle.
		/// </summary>
		/// <typeparam name="T">The type of the asset to load.</typeparam>
		/// <param name="name">The name of the asset to load.</param>
		/// <returns>Loaded asset.</returns>
		public static T LoadAsset<T>(string name) where T : UnityEngine.Object {
			return assetBundle.LoadAsset<T>(name);
		}

		/// <summary>
		/// Call this when fatal error occurs. This will print error into the log and close the game.
		/// </summary>
		/// <param name="message">The message to print to console.</param>
		public static void FatalError(string message) {
			Logger.Log(message);
			Logger.Log(Environment.StackTrace);
#if DEBUG
			if (Debugger.IsAttached) {
				throw new System.Exception(message);
			}
			else {
#endif
				Process.GetCurrentProcess().Kill();
#if DEBUG
			}
#endif
		}

		/// <summary>
		/// Standard assertion. If given condition is not true then prints message to the log and closes game.
		/// </summary>
		/// <param name="condition">Condition to chec.</param>
		/// <param name="message">The message to print to console.</param>
		public static void Assert(bool condition, string message) {
			if (condition) {
				return;
			}
			Logger.Log("[ASSERTION FAILED]");
			FatalError(message);
		}
	}
}
