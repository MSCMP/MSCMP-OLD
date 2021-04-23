using MSCLoader;

namespace MSCMP {
	public class MSCMPMod : Mod {
		// The ID of the mod - Should be unique

		public override string ID => "MPMod";

		// The name of the mod that is displayed
		public override string Name => "MP Mod";

		public override bool UseAssetsFolder => true;

		// The name of the author

		// The name of the author
		public override string Author => "unsigned_void, xz.wache, H17RO";

		// The version of the mod - whatever you want.
		public override string Version => "1.0";

		public override bool LoadInMenu => true;

		// Called when the mod is loaded
		public override void OnMenuLoad() {
			SetupLogger();
			Client.Start();
		}

		private void SetupLogger() {
			Logger.SetupLogger();
			if (Logger.IsInitialized == false) {
				Logger.Warning("Logger is not initialized correctly. All info will be displayed only in Console.");
			}
		}
	}
}
