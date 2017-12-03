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
	}
}
