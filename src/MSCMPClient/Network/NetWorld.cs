using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSCMP.Network {
	class NetWorld {

		/// <summary>
		/// Maximum count of the supported vehicles.
		/// </summary>
		public const int MAX_VEHICLES = Byte.MaxValue;

		/// <summary>
		/// Maximum count of the supported pickupables.
		/// </summary>
		public const int MAX_PICKUPABLES = UInt16.MaxValue;

		/// <summary>
		/// Network vehicles pool.
		/// </summary>

		List<NetVehicle> vehicles = new List<NetVehicle>();

		/// <summary>
		/// Net manager owning this world.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// Network pickupables pool.
		/// </summary>
		Dictionary<ushort, NetPickupable> netPickupables = new Dictionary<ushort, NetPickupable>();

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">Network manager owning this network world.</param>
		public NetWorld(NetManager netManager) {
			this.netManager = netManager;

			// Register all network vehicles.

			RegisterVehicle("JONNEZ ES(Clone)");
			RegisterVehicle("HAYOSIKO(1500kg, 250)");
			RegisterVehicle("SATSUMA(557kg, 248)");
			RegisterVehicle("RCO_RUSCKO12(270)");
			RegisterVehicle("KEKMET(350-400psi)");
			RegisterVehicle("FLATBED");
			RegisterVehicle("FERNDALE(1630kg)");

			Game.GameCallbacks.onWorldUnload += () => {
				OnGameWorldUnload();
			};

			Game.GameCallbacks.onWorldLoad += () => {
				OnGameWorldLoad();
			};

			Game.GameCallbacks.onPlayMakerObjectCreate += (GameObject instance, GameObject pickupable) => {
				if (!Game.GamePickupableDatabase.IsPickupable(instance)) {
					return;
				}

				var metaData = pickupable.GetComponent<Game.Components.PickupableMetaDataComponent>();
				Client.Assert(metaData != null, "Tried to spawn pickupable that has no meta data assigned.");

				ushort freeId = FindFreePickpableId();
				Client.Assert(freeId != NetPickupable.INVALID_ID, "Out of pickupables pool");
				RegisterPickupable(freeId, instance);

				Messages.PickupableSpawnMessage msg = new Messages.PickupableSpawnMessage();
				msg.id = freeId;
				msg.prefabId = metaData.prefabId;
				msg.transform.position = Utils.GameVec3ToNet(instance.transform.position);
				msg.transform.rotation = Utils.GameQuatToNet(instance.transform.rotation);
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};


			Game.GameCallbacks.onPlayMakerObjectActivate += (GameObject instance, bool activate) => {
				if (!Game.GamePickupableDatabase.IsPickupable(instance)) {
					return;
				}

				NetPickupable pickupable = GetPickupableByGameObject(instance);
				if (pickupable == null) {
					return;
				}

				if (activate) {
					var metaData = pickupable.gameObject.GetComponent<Game.Components.PickupableMetaDataComponent>();

					Messages.PickupableSpawnMessage msg = new Messages.PickupableSpawnMessage();
					msg.id = pickupable.NetId;
					msg.prefabId = metaData.prefabId;
					msg.transform.position = Utils.GameVec3ToNet(instance.transform.position);
					msg.transform.rotation = Utils.GameQuatToNet(instance.transform.rotation);
					netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
				}
				else {
					Messages.PickupableActivateMessage msg = new Messages.PickupableActivateMessage();
					msg.id = pickupable.NetId;
					msg.activate = false;
					netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
				}
			};

			netManager.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableActivateMessage msg) => {
				GameObject gameObject = null;
				if (netPickupables.ContainsKey(msg.id)) {
					gameObject = netPickupables[msg.id].gameObject;
				}

				if (msg.activate) {
					Client.Assert(gameObject != null, "Tried to activate pickupable but its not spawned!");
					gameObject.SetActive(true);
				}
				else {
					if (gameObject != null) {
						gameObject.SetActive(false);
					}
				}
			});

			netManager.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableSpawnMessage msg) => {
				SpawnPickupable(msg);
			});

			netManager.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableDestroyMessage msg) => {
				Client.Assert(netPickupables.ContainsKey(msg.id), "Invalid pickupable id in destroy message.");

				NetPickupable pickupable = netPickupables[msg.id];
				GameObject.Destroy(pickupable.gameObject);
				netPickupables.Remove(msg.id);
			});
		}

		/// <summary>
		/// Register vehicle into network vehicles pool.
		/// </summary>
		/// <param name="gameObjectName"></param>
		private void RegisterVehicle(string gameObjectName) {
			if (vehicles.Count == MAX_VEHICLES) {
				throw new Exception("Out of vehicle pool!");
			}

			byte netId = (byte) vehicles.Count;
			vehicles.Add(new NetVehicle(netManager, gameObjectName, netId));
		}

		/// <summary>
		/// Get vehicle by it's network id.
		/// </summary>
		/// <param name="netId">Network id of the vehicle.</param>
		/// <returns>Network vehicle object.</returns>
		public NetVehicle GetVehicle(byte netId) {
			return vehicles[netId];
		}

		/// <summary>
		/// Update net world.
		/// </summary>
		public void Update() {
		}


		/// <summary>
		/// FixedUpdate net world.
		/// </summary>
		public void FixedUpdate() {
			foreach (var v in vehicles) {
				v.FixedUpdate();
			}
		}

		/// <summary>
		/// Called when game world gets loaded.
		/// </summary>
		public void OnGameWorldLoad() {
			RegisterPickupables();

			// Update vehicles.

			foreach (var vehicle in vehicles) {
				vehicle.OnGameWorldLoad();
			}
		}

		/// <summary>
		/// Find free pickupable id.
		/// </summary>
		/// <returns>Pickupable id or NetPickupable.INVALID_ID when no free ID was found.</returns>
		private ushort FindFreePickpableId() {
			for (ushort i = 0; i < MAX_PICKUPABLES; ++i) {
				if (!netPickupables.ContainsKey(i)) {
					return i;
				}
			}
			return NetPickupable.INVALID_ID;
		}


		/// <summary>
		/// Register all pickupables that are in the game world.
		/// </summary>
		public void RegisterPickupables() {
			netPickupables.Clear();

			var pickupables = Game.GamePickupableDatabase.Instance.CollectAllPickupables(false);
			foreach (var pickupable in pickupables) {
				if (netPickupables.Count == MAX_PICKUPABLES) {
					throw new Exception("Out of pickupables pool!");
				}

				RegisterPickupable((ushort)netPickupables.Count, pickupable);
			}
		}


		/// <summary>
		/// Called when game world gets unloaded.
		/// </summary>
		private void OnGameWorldUnload() {
			netPickupables.Clear();
		}


		/// <summary>
		/// Write full world synchronization message.
		/// </summary>
		/// <param name="msg">The message to write to.</param>
		public void WriteFullWorldSync(Messages.FullWorldSyncMessage msg) {
			// Write doors

			List<Game.Objects.GameDoor> doors = Game.GameDoorsManager.Instance.doors;
			int doorsCount = doors.Count;
			msg.doors = new Messages.DoorsInitMessage[doorsCount];

			for (int i = 0; i < doorsCount; ++i) {
				var doorMsg = new Messages.DoorsInitMessage();
				Game.Objects.GameDoor door = doors[i];
				doorMsg.position  = Utils.GameVec3ToNet(door.Position);
				doorMsg.open = door.IsOpen;
				msg.doors[i] = doorMsg;
			}

			// Write vehicles.

			int vehiclesCount = vehicles.Count;
			msg.vehicles = new Messages.VehicleInitMessage[vehiclesCount];

			for (int i = 0; i < vehiclesCount; ++i) {
				var vehicleMsg = new Messages.VehicleInitMessage();
				NetVehicle vehicle = vehicles[i];
				vehicleMsg.id = vehicle.NetId;
				vehicleMsg.transform.position = Utils.GameVec3ToNet(vehicle.GetPosition());
				vehicleMsg.transform.rotation = Utils.GameQuatToNet(vehicle.GetRotation());
				msg.vehicles[i] = vehicleMsg;
			}

			// Write pickupables.

			msg.pickupables = new Messages.PickupableSpawnMessage[netPickupables.Count];
			int idx = 0;
			foreach (var kv in netPickupables) {
				NetPickupable pickupable = kv.Value;
				var pickupableMsg = new Messages.PickupableSpawnMessage();
				pickupableMsg.id = pickupable.NetId;
				var metaData = pickupable.gameObject.GetComponent<Game.Components.PickupableMetaDataComponent>();
				pickupableMsg.prefabId = metaData.prefabId;
				Transform transform = pickupable.gameObject.transform;
				pickupableMsg.transform.position = Utils.GameVec3ToNet(transform.position);
				pickupableMsg.transform.rotation = Utils.GameQuatToNet(transform.rotation);
				msg.pickupables[idx++] = pickupableMsg;
			}
		}


		/// <summary>
		/// Handle full world sync message.
		/// </summary>
		/// <param name="msg">The message to handle.</param>

		public void HandleFullWorldSync(Messages.FullWorldSyncMessage msg) {
			// Doors.

			foreach (Messages.DoorsInitMessage door in msg.doors) {
				Vector3 position = Utils.NetVec3ToGame(door.position);
				Game.Objects.GameDoor doors = Game.GameDoorsManager.Instance.FindGameDoors(position);
				Client.Assert(doors != null, $"Unable to find doors at: {position}.");
				if (doors.IsOpen != door.open) {
					doors.Open(door.open);
				}
			}

			// Vehicles.

			foreach (Messages.VehicleInitMessage vehicleMsg in msg.vehicles) {
				Vector3 pos = Utils.NetVec3ToGame(vehicleMsg.transform.position);
				Quaternion rot = Utils.NetQuatToGame(vehicleMsg.transform.rotation);

				NetVehicle vehicle = GetVehicle(vehicleMsg.id);
				Client.Assert(vehicle != null, $"Received info about non existing vehicle {vehicleMsg.id} in full world sync. (pos: {pos}, rot: {rot})");

				vehicle.Teleport(pos, rot);
			}

			// Pickupables

			List<ushort> pickupablesIds = new List<ushort>();
			foreach (var kv in netPickupables) {
				pickupablesIds.Add(kv.Key);
			}

			foreach (Messages.PickupableSpawnMessage pickupableMsg in msg.pickupables) {
				SpawnPickupable(pickupableMsg);
				pickupablesIds.Remove(pickupableMsg.id);
			}

			// Remove spawned (and active) pickupables that we did not get info about.

			foreach (ushort id in pickupablesIds) {
				GameObject gameObject = netPickupables[id].gameObject;
				if (!gameObject.activeSelf) {
					continue;
				}
				DestroyPickupableLocal(id);
			}
		}

		/// <summary>
		/// Ask host for full world sync.
		/// </summary>
		public void AskForFullWorldSync() {
			Messages.AskForWorldStateMessage msg = new Messages.AskForWorldStateMessage();
			netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Update debug imgui.
		/// </summary>
		public void UpdateIMGUI() {
			foreach (var v in vehicles) {
				v.UpdateIMGUI();
			}
		}
#endif

		/// <summary>
		/// Get pickupable game object from network id.
		/// </summary>
		/// <param name="netId">Network id of the pickupable.</param>
		/// <returns>Game object representing the given pickupable or null if there is no pickupable matching this network id.</returns>
		public GameObject GetPickupableGameObject(ushort netId) {
			if (netPickupables.ContainsKey(netId)) {
				return netPickupables[netId].gameObject;
			}
			return null;
		}

		/// <summary>
		/// Get pickupable network id from game object.
		/// </summary>
		/// <param name="go">Game object to get pickupable it for.</param>
		/// <returns>The network id of pickupable or invalid id of the pickupable if not pickupable is found for given game object.</returns>
		public NetPickupable GetPickupableByGameObject(GameObject go) {
			foreach (var pickupable in netPickupables) {
				if (pickupable.Value.gameObject == go) {
					return pickupable.Value;
				}
			}
			return null;
		}

		/// <summary>
		/// Get pickupable network id from game object.
		/// </summary>
		/// <param name="go">Game object to get pickupable it for.</param>
		/// <returns>The network id of pickupable or invalid id of the pickupable if not pickupable is found for given game object.</returns>
		public ushort GetPickupableNetId(GameObject go) {
			var pickupable = GetPickupableByGameObject(go);
			if (pickupable != null) {
				return pickupable.NetId;
			}
			return NetPickupable.INVALID_ID;
		}

		/// <summary>
		/// Handle destroy of pickupable game object.
		/// </summary>
		/// <param name="pickupable">The destroyed pickupable.</param>
		public void HandlePickupableDestroy(GameObject pickupable) {
			var netPickupable = GetPickupableByGameObject(pickupable);
			if (netPickupable != null) {
				Messages.PickupableDestroyMessage msg = new Messages.PickupableDestroyMessage();
				msg.id = netPickupable.NetId;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);

				Logger.Log($"Handle pickupable destroy {pickupable.name}");
				netPickupables.Remove(netPickupable.NetId);
			}
			else {
				Logger.Log($"Unhandled pickupable has been destroyed {pickupable.name}");
				Logger.Log(Environment.StackTrace);
			}
		}

		/// <summary>
		/// Spawn pickupable from network message.
		/// </summary>
		/// <param name="msg">The message containing info about pickupable to spawn.</param>
		public void SpawnPickupable(Messages.PickupableSpawnMessage msg) {
			Vector3 position = Utils.NetVec3ToGame(msg.transform.position);
			Quaternion rotation = Utils.NetQuatToGame(msg.transform.rotation);

			if (netPickupables.ContainsKey(msg.id)) {
				NetPickupable netPickupable = netPickupables[msg.id];
				GameObject gameObject = netPickupable.gameObject;
				var metaData = gameObject.GetComponent<Game.Components.PickupableMetaDataComponent>();
				if (msg.prefabId == metaData.prefabId) {
					gameObject.SetActive(true);
					gameObject.transform.position = position;
					gameObject.transform.rotation = rotation;
					return;
				}
				else {
					DestroyPickupableLocal(msg.id);
				}
			}

			GameObject pickupable = Game.GameWorld.Instance.SpawnPickupable(msg.prefabId, position, rotation);
			RegisterPickupable(msg.id, pickupable);
		}

		/// <summary>
		/// Destroy given pickupable from the game without sending destroy message to players.
		/// </summary>
		/// <param name="id">The network id of the pickupable to destroy.</param>
		private void DestroyPickupableLocal(ushort id) {
			if (!netPickupables.ContainsKey(id)) {
				return;
			}
			var gameObject = netPickupables[id].gameObject;
			var lifeTracker = gameObject.AddComponent<Game.Components.PickupableLifeTrackerComponent>();
			lifeTracker.netWorld = null;
			GameObject.Destroy(gameObject);
			netPickupables.Remove(id);
		}

		/// <summary>
		/// Register pickupable into the network world.
		/// </summary>
		/// <param name="netId">The network id of the pickupable.</param>
		/// <param name="pickupable">The game object representing pickupable.</param>
		public void RegisterPickupable(ushort netId, GameObject pickupable) {
			Client.Assert(!netPickupables.ContainsKey(netId), $"Duplicate net id {netId}");
			var metaData = pickupable.GetComponent<Game.Components.PickupableMetaDataComponent>();
			Client.Assert(metaData != null, $"Failed to register pickupable. No meta data found. {pickupable.name} ({pickupable.GetInstanceID()})");

			Logger.Log($"Registering pickupable {pickupable.name} (net id: {netId}, instance id: {pickupable.GetInstanceID()})");

			var lifeTracker = pickupable.AddComponent<Game.Components.PickupableLifeTrackerComponent>();
			lifeTracker.netWorld = this;

			netPickupables.Add(netId, new NetPickupable(netId, pickupable));
		}
	}
}
