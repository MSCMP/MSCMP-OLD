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
			// Register all pickupables.

			netPickupables.Clear();

			var pickupables = Game.GameWorld.Instance.CollectAllPickupables();
			foreach (var pickupable in pickupables) {
				if (netPickupables.Count == MAX_PICKUPABLES) {
					throw new Exception("Out of pickupables pool!");
				}

				// HACK: As for now destroy xmas present. Xmas present is only not removed as specific
				// day - PlayMaker script placed in "Database > Xmas Present " destroys it after loading game
				// when the date is not correct.
				//
				// TODO: Database of pickupables - collect all pickupables that we will sync - copy them and deactivate them.
				// Then activate only pickupables that host reported should be placed in the world.

				if (pickupable.name.StartsWith("xmas present")) {
					GameObject.Destroy(pickupable);
					continue;
				}

				ushort netId = (ushort) netPickupables.Count;
				netPickupables.Add(netId, new NetPickupable(netId, pickupable));
				Logger.Log("Register pickupable " + pickupable.name + " (net id: " + netId + ")");
			}

			// Update vehicles.

			foreach (var vehicle in vehicles) {
				vehicle.OnGameWorldLoad();
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

			msg.pickupables = new Messages.PickupableInitMessage[netPickupables.Count];

			int idx = 0;
			foreach (var kv in netPickupables) {
				NetPickupable pickupable = kv.Value;

				var pickupableMsg = new Messages.PickupableInitMessage();
				pickupableMsg.id = pickupable.NetId;
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

			foreach (Messages.PickupableInitMessage pickupableMsg in msg.pickupables) {
				Vector3 position = Utils.NetVec3ToGame(pickupableMsg.transform.position);
				Quaternion rotation = Utils.NetQuatToGame(pickupableMsg.transform.rotation);
				ushort netId = pickupableMsg.id;
				Client.Assert(netPickupables.ContainsKey(netId), $"Received init state of the pickupable that does not exists. {netId} [pos: {position}, rot: {rotation}]");

				GameObject pickupableGameObject = netPickupables[pickupableMsg.id].gameObject;
				if (pickupableGameObject == null) {
					Logger.Log($"Tried to set position of not spawned pickupable {netId}");
					continue;
				}
				pickupableGameObject.transform.position = position;
				pickupableGameObject.transform.rotation = rotation;
			}

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
		public ushort GetPickupableNetId(GameObject go) {
			foreach (var pickupable in netPickupables) {
				if (pickupable.Value.gameObject == go) {
					return pickupable.Key;
				}
			}
			return NetPickupable.INVALID_ID;
		}
	}
}
