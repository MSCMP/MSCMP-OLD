using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using System;
using MSCMP.Utilities;

namespace MSCMP {
	/// <summary>
	/// Main class of the mod.
	/// </summary>
	public class Client {

		/// <summary>
		/// The current mod development stage.
		/// </summary>
		public const string MOD_DEVELOPMENT_STAGE = "Pre-Alpha";

		/// <summary>
		/// Asset bundle containing multiplayer mod content.
		/// </summary>
		private static AssetBundle _assetBundle = null;

		/// <summary>
		/// The my summer car game app id.
		/// </summary>
		public static readonly Steamworks.AppId_t GAME_APP_ID =
				new Steamworks.AppId_t(516750);

		/// <summary>
		/// Starts the mod. Called from Injector.
		/// </summary>
		public static void Start() {
			// Game.Hooks.PlayMakerActionHooks.Install();
			var bundleFolderPath = "Mods/Assets/MPMod/mpdata";
			var assetBundlePath = ModUtils.GetPath(bundleFolderPath);

			if (!File.Exists(assetBundlePath)) {
				FatalError("Cannot find mpdata asset bundle.");
				return;
			}

			_assetBundle = AssetBundle.CreateFromFile(assetBundlePath);

			var go = new GameObject("Multiplayer GUI Controller");
			go.AddComponent<UI.MPGUI>();

			go = new GameObject("Multiplayer Controller");
			go.AddComponent<MPController>();

			UI.Console.RegisterCommand("quit", (string[] args) => { Application.Quit(); });
		}

		/// <summary>
		/// Loads asset from multiplayer mod asset bundle.
		/// </summary>
		/// <typeparam name="T">The type of the asset to load.</typeparam>
		/// <param name="name">The name of the asset to load.</param>
		/// <returns>Loaded asset.</returns>
		public static T LoadAsset<T>(string name)
				where T : UnityEngine.Object => _assetBundle.LoadAsset<T>(name);

		/// <summary>
		/// Call this when fatal error occurs. This will print error into the log and
		/// close the game.
		/// </summary>
		/// <param name="message">The message to print to console.</param>
		public static void FatalError(string message) {
			Logger.Log(message);
			Logger.Log(Environment.StackTrace);

#if DEBUG
			if (Debugger.IsAttached) {
				throw new Exception(message);
			} else {
#endif
				Process.GetCurrentProcess().Kill();
#if DEBUG
			}
#endif
		}

		/// <summary>
		/// Standard assertion. If given condition is not true then prints message to the
		/// log and closes game.
		/// </summary>
		/// <param name="condition">Condition to chec.</param>
		/// <param name="message">The message to print to console.</param>
		public static void Assert(bool condition, string message) {
			if (condition) { return; }
			Logger.Log("[ASSERTION FAILED]");
			FatalError(message);
		}

		/// <summary>
		/// Get display version of the mod.
		/// </summary>
		/// <returns></returns>
		public static string GetMODDisplayVersion() {
			string version = Assembly.GetExecutingAssembly()
													 .GetName()
													 .Version.ToString();
			version += " " + MOD_DEVELOPMENT_STAGE;
			return version;
		}

		/// <summary>
		/// Add message to the console.
		/// </summary>
		/// <param name="message">The message to add.</param>
		static public void ConsoleMessage(string message) {
			if (UI.Console.Instance != null) { UI.Console.Instance.AddMessage(message); }
		}
	}
}
