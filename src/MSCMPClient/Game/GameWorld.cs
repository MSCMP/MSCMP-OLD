
using System.Collections.Generic;

using MSCMP.Game.Objects;

namespace MSCMP.Game {

	/// <summary>
	/// Object managing state of the game world.
	/// </summary>
	class GameWorld {

		/// <summary>
		/// Doors manager.
		/// </summary>
		private GameDoorsManager doorsManager = new GameDoorsManager();

		public GameWorld() {

		}

		/// <summary>
		/// Callback called when world is loaded.
		/// </summary>
		public void OnLoad() {
			doorsManager.OnWorldLoad();
		}
	}
}
