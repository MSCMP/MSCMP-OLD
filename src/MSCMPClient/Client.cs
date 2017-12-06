using System.IO;
using System.Reflection;
using UnityEngine;

namespace MSCMP
{
	/// <summary>
	/// Main class of the mod.
	/// </summary>
	public class Client {

		/// <summary>
		/// Starts the mod. Called from Injector.
		/// </summary>
		public static void Start() {
			Logger.SetAutoFlush(true);
			GameObject go = new GameObject("Multiplayer Controller");
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
		/// Call this when fatal error occurs. This will print error into the log and close the game.
		/// </summary>
		/// <param name="message">The message to print to console.</param>
		public static void FatalError(string message) {
			Logger.Log(message);
			Application.Quit();
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
			Logger.Log(message);
			Application.Quit();
		}
	}
}
