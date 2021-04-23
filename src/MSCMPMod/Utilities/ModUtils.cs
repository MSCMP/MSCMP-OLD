using System.IO;
using System.Reflection;

namespace MSCMP.Utilities {
	public static class ModUtils {

		private static string _gamePath = string.Empty;

		public static string GetGamePath() {
			return string.IsNullOrEmpty(_gamePath) ? _gamePath : GetPath("");
		}
		
		/// <summary>
		/// Gets absolute path for the specified file relative to mod installation
		/// folder.
		/// </summary>
		/// <param name="file">The file to get path for.</param>
		/// <returns>Absolute path for the specified file relative to mod instalation
		/// folder.</returns>
		public static string GetPath(string path) {
			if (!string.IsNullOrEmpty(_gamePath)) {
				return Path.Combine(_gamePath, path);
			}

			string managedFolder =
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string gameFolder =
				Directory.GetParent(managedFolder).Parent.ToString();

			_gamePath = gameFolder;
			return Path.Combine(_gamePath, path);

		}
	}
}
