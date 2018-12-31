using MSCMP.Game.Components;
using System.Collections.Concurrent;
using UnityEngine;

namespace MSCMP.Game {
	/// <summary>
	/// Class managing sync of objects.
	/// </summary>
	class ObjectSyncManager {
		/// <summary>
		/// Instance.
		/// </summary>
		public static ObjectSyncManager Instance = null;

		/// <summary>
		/// Dictionary of ObjectIDs.
		/// </summary>
		public ConcurrentDictionary<int, ObjectSyncComponent> ObjectIDs = new ConcurrentDictionary<int, ObjectSyncComponent>();

		/// <summary>
		/// Type of objects.
		/// </summary>
		public enum ObjectTypes {
			Pickupable,
			PlayerVehicle,
			AIVehicle,
			Boat,
			GarageDoor,
		}

		/// <summary>
		/// Sync types
		/// </summary>
		public enum SyncTypes {
			GenericSync,
			SetOwner,
			RemoveOwner,
			ForceSetOwner,
			PeriodicSync,
		}

		/// <summary>
		/// Used when adding an ObjectSyncComponent for an ObjectID to be automatically assigned.
		/// </summary>
		public static int AUTOMATIC_ID = -1;

		/// <summary>
		/// Used when checking ownership of a ObjectSyncComponent snyced object.
		/// </summary>
		public static ulong NO_OWNER = 0;

		/// <summary>
		/// Local player's Steam ID.
		/// </summary>
		public Steamworks.CSteamID steamID;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ObjectSyncManager() {
			Instance = this;
		}

		/// <summary>
		/// Adds new object to the ObjectIDs Dictionary.
		/// </summary>
		/// <param name="osc">Object to add.</param>
		/// <param name="objectID">Object ID to assign to object.</param>
		/// <returns>ObjectID of object.</returns>
		public int AddNewObject(ObjectSyncComponent osc, int objectID) {
			// Assign ObjectID automatically.
			if (objectID == AUTOMATIC_ID) {
				if (steamID.m_SteamID == 0) {
					steamID = Steamworks.SteamUser.GetSteamID();
				}
				Logger.Debug($"Added new ObjectID at: {ObjectIDs.Count + 1}");
				ObjectIDs.GetOrAdd(ObjectIDs.Count + 1, osc);
				return ObjectIDs.Count;
			}
			// Assign object a specific ObjectID.
			else {
				Logger.Debug($"Force adding new ObjectID at: {objectID}");
				if (ObjectIDs.ContainsKey(objectID)) {
					ObjectIDs[objectID] = osc;
				}
				else {
					ObjectIDs.GetOrAdd(objectID, osc);
				}
				return objectID;
			}
		}

		/// <summary>
		/// Check if a periodic object sync should be performed.
		/// </summary>
		/// <returns>True if object periodic sync should be sent.</returns>
		public bool ShouldPeriodicSync(ulong owner, bool syncEnabled) {
			if (Network.NetManager.Instance.TicksSinceConnectionStarted % 500 == 0) {
				if (syncEnabled || owner == NO_OWNER && Network.NetManager.Instance.IsHost) {
					return true;
				}
			}
			return false;
		}
	}
}
