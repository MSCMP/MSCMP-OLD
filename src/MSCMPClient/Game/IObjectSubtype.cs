using UnityEngine;

namespace MSCMP.Game {
	interface IObjectSubtype {

		/// <summary>
		/// Return variables to be included in the object sync.
		/// </summary>
		/// <returns>Variables to sync.</returns>
		float[] ReturnSyncedVariables();

		/// <summary>
		/// Handle synced variables from the remote client.
		/// </summary>
		/// <param name="variables">Synced variables from the remote client.</param>
		void HandleSyncedVariables(float[] variables);

		/// <summary>
		/// If the object should sync the variables.
		/// </summary>
		/// <returns>True is object should sync variables.</returns>
		bool CanSync();
	}
}
