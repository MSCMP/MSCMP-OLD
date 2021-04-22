using MSCLoader;
using UnityEngine;

namespace MSCMP {
	public class MSCMPMod : Mod {
		// The ID of the mod - Should be unique
		public override string ID => "MPMod";

		// The name of the mod that is displayed
		public override string Name => "MP Mod";

		// The name of the author
		public override string Author => "unsigned_void";

		// The version of the mod - whatever you want.
		public override string Version => "1.0";

		// Called when the mod is loaded
		public override void OnLoad() {
			ModConsole.Print("ExampleMod has been loaded!"); // print debug information
		}
	}
}
