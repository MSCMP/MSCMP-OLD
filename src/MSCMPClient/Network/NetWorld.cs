using System;
using System.Collections.Generic;


namespace MSCMP.Network {
	class NetWorld {

		/// <summary>
		/// Maximum count of the supported vehicles.
		/// </summary>
		public const int MAX_VEHICLES = Byte.MaxValue;

		/// <summary>
		/// Network vehicles pool.
		/// </summary>

		List<NetVehicle> vehicles = new List<NetVehicle>();

		/// <summary>
		/// Net manager owning this world.
		/// </summary>
		NetManager netManager = null;


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
			foreach (var vehicle in vehicles) {
				vehicle.OnGameWorldLoad();
			}
		}

		public void WriteFullWorldSync(Messages.FullWorldSyncMessage msg) {
			// Write doors

			List<Game.Objects.GameDoor> doors = Game.GameDoorsManager.Instance.doors;
			int doorsCount = doors.Count;
			msg.doorsOpen = new bool[doorsCount];
			msg.doorsPosition = new Messages.Vector3Message[doorsCount];

			for (int i = 0; i < doorsCount; ++i) {
				Game.Objects.GameDoor door = doors[i];
				msg.doorsPosition[i] = Utils.GameVec3ToNet(door.Position);
				msg.doorsOpen[i] = door.IsOpen;
			}

			// Write vehicles.

			int vehiclesCount = vehicles.Count;
			msg.vehicleId = new byte[vehiclesCount];
			msg.vehiclesPosition = new Messages.Vector3Message[vehiclesCount];
			msg.vehiclesRotation = new Messages.QuaternionMessage[vehiclesCount];

			for (int i = 0; i < vehiclesCount; ++i) {
				NetVehicle vehicle = vehicles[i];
				msg.vehicleId[i] = vehicle.NetId;
				msg.vehiclesPosition[i] = Utils.GameVec3ToNet(vehicle.GetPosition());
				msg.vehiclesRotation[i] = Utils.GameQuatToNet(vehicle.GetRotation());
			}
		}

		public void UpdateIMGUI() {
			foreach (var v in vehicles) {
				v.UpdateIMGUI();
			}
		}
	}
}
