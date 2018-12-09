using UnityEngine;

namespace MSCMP.Game {
	interface ISyncedObject {

		/// <summary>
		/// Transform of the object.
		/// </summary>
		/// <returns>Object's Transform.</returns>
		Transform ObjectTransform();

		/// <summary>
		/// Check is periodic sync of the object is enabled.
		/// </summary>
		/// <returns>Periodic sync enabled or disabled.</returns>
		bool PeriodicSyncEnabled();

		/// <summary>
		/// Called to determine if the object should be synced. 
		/// </summary>
		/// <returns>True if object should be synced, false if object shouldn't be synced.</returns>
		bool CanSync();

		/// <summary>
		/// Called when a player enters range of an object.
		/// </summary>
		/// <returns>True if the player should tkae ownership of the object.</returns>
		bool ShouldTakeOwnership();

		/// <summary>
		/// Called to return variables that need to be synced on the remote client.
		/// </summary>
		/// <returns>Variables to send to remote client.</returns>
		/// <param name="sendFullSync">Send all variables regardless of conditions.</param>
		float[] ReturnSyncedVariables(bool sendFullSync);

		/// <summary>
		/// Handle synced variables sent from the remote client.
		/// </summary>
		/// <param name="variables"></param>
		void HandleSyncedVariables(float[] variables);

		/// <summary>
		/// Called when owner is set to remote client.
		/// </summary>
		void OwnerSetToRemote();

		/// <summary>
		/// Called when owner is removed.
		/// </summary>
		void OwnerRemoved();

		/// <summary>
		/// Called when sync is forcefully taken from client.
		/// </summary>
		void SyncTakenByForce();

		/// <summary>
		/// Called when an object is constantly syncing. (Usually when a pickupable is picked up, or when a vehicle is being driven)
		/// </summary>
		/// <param name="newValue"></param>
		void ConstantSyncChanged(bool newValue);
	}
}
