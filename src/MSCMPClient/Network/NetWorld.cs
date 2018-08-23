using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MSCMP.Game.Components;
using MSCMP.Game.Objects;
using MSCMP.Game;

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
		/// Interval between each periodical update in seconds.
		/// </summary>
		const float PERIODICAL_UPDATE_INTERVAL = 10.0f;

		/// <summary>
		/// Time left to send periodical message.
		/// </summary>
		float timeToSendPeriodicalUpdate = 0.0f;

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
			RegisterVehicle("GIFU(750/450psi)");

			//RegisterVehicle("HAYOSIKO(1500kg, 250)(Clone)");

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
				msg.active = instance.activeSelf;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};


			Game.GameCallbacks.onPlayMakerObjectActivate += (GameObject instance, bool activate) => {
				if (activate == instance.activeSelf) {
					return;
				}

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

			Game.GameCallbacks.onPlayMakerObjectDestroy += (GameObject instance) => {
				if (!Game.GamePickupableDatabase.IsPickupable(instance)) {
					return;
				}

				NetPickupable pickupable = GetPickupableByGameObject(instance);
				if (pickupable == null) {
					Logger.Debug($"Pickupable {instance.name} has been destroyed however it is not registered, skipping removal.");
					return;
				}

				HandlePickupableDestroy(instance);
			};

			Game.GameCallbacks.onPlayMakerSetPosition += (GameObject gameObject, Vector3 position, Space space) => {
				if (!Game.GamePickupableDatabase.IsPickupable(gameObject)) {
					return;
				}

				NetPickupable pickupable = GetPickupableByGameObject(gameObject);
				if (pickupable == null) {
					return;
				}


				if (space == Space.Self) {
					position += gameObject.transform.position;
				}

				Messages.PickupableSetPositionMessage msg = new Messages.PickupableSetPositionMessage();
				msg.id = pickupable.NetId;
				msg.position = Utils.GameVec3ToNet(position);
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
			};

			RegisterNetworkMessagesHandlers(netManager.MessageHandler);
		}

		/// <summary>
		/// Register world related network message handlers.
		/// </summary>
		/// <param name="netMessageHandler">The network message handler to register messages to.</param>
		void RegisterNetworkMessagesHandlers(NetMessageHandler netMessageHandler) {
			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableSetPositionMessage msg) => {
				Client.Assert(netPickupables.ContainsKey(msg.id), $"Tried to move pickupable that is not spawned {msg.id}.");
				GameObject gameObject = netPickupables[msg.id].gameObject;
				gameObject.transform.position = Utils.NetVec3ToGame(msg.position);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableActivateMessage msg) => {
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

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableSpawnMessage msg) => {
				SpawnPickupable(msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupableDestroyMessage msg) => {
				if (!netPickupables.ContainsKey(msg.id)) {
					return;
				}

				NetPickupable pickupable = netPickupables[msg.id];
				GameObject.Destroy(pickupable.gameObject);
				netPickupables.Remove(msg.id);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.WorldPeriodicalUpdateMessage msg) => {
				// Game reports 'next hour' - we want to have transition so correct it.
				Game.GameWorld.Instance.WorldTime = (float)msg.sunClock - 2.0f;
				Game.GameWorld.Instance.WorldDay = (int)msg.worldDay;
				Game.GameWeatherManager.Instance.SetWeather(msg.currentWeather);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.RemoveBottleMessage msg) => {
				GameObject beerGO = GetPickupableGameObject(msg.netId);
				Game.Objects.BeerCase beer = Game.BeerCaseManager.Instance.FindBeerCase(beerGO);
				if (beer == null) {
					Logger.Log($"Player tried to drink beer, however, the beercase cannot be found.");
					return;
				}
				beer.RemoveBottles(1);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PlayerSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Log($"Received synchronization packet from {sender} but there is not player registered using this id.");
					return;
				}

				player.HandleSynchronize(msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.AnimSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null)
				{
					Logger.Log($"Received animation synchronization packet from {sender} but there is not player registered using this id.");
					return;
				}

				player.HandleAnimSynchronize(msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.OpenDoorsMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Log($"Received OpenDoorsMessage however there is no matching player {sender}! (open: {msg.open}");
					return;
				}

				Game.Objects.GameDoor doors = Game.GameDoorsManager.Instance.FindGameDoors(Utils.NetVec3ToGame(msg.position));
				if (doors == null) {
					Logger.Log($"Player tried to open door, however, the door could not be found!");
					return;
				}
				doors.Open(msg.open);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.FullWorldSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);

				// This one should never happen - if happens there is something done miserably wrong.
				Client.Assert(player != null, $"There is no player matching given steam id {sender}.");

				// Handle full world state synchronization.

				HandleFullWorldSync(msg);

				// Spawn host character.

				player.Spawn();

				// Set player state.

				player.Teleport(Utils.NetVec3ToGame(msg.spawnPosition), Utils.NetQuatToGame(msg.spawnRotation));

				if (msg.occupiedVehicleId != NetVehicle.INVALID_ID) {
					var vehicle = GetVehicle(msg.occupiedVehicleId);
					Client.Assert(vehicle != null, $"Player {player.GetName()} ({player.SteamId}) you tried to join reported that he drives car that does not exists in your game. Vehicle id: {msg.occupiedVehicleId}, passenger: {msg.passenger}");
					player.EnterVehicle(vehicle, msg.passenger);
				}

				if (msg.pickedUpObject != NetPickupable.INVALID_ID) {
					player.PickupObject(msg.pickedUpObject);
				}

				// World is loaded! Notify network manager about that.

				netManager.OnNetworkWorldLoaded();
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.AskForWorldStateMessage msg) => {
				var msgF = new Messages.FullWorldSyncMessage();
				WriteFullWorldSync(msgF);
				netManager.BroadcastMessage(msgF, Steamworks.EP2PSend.k_EP2PSendReliable);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.VehicleEnterMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error($"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}

				NetVehicle vehicle = GetVehicle(msg.vehicleId);
				if (vehicle == null) {
					Logger.Error("Player " + player.SteamId + " tried to enter vehicle " + msg.vehicleId + " but there is no vehicle with such id.");
					return;
				}

				player.EnterVehicle(vehicle, msg.passenger);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.VehicleLeaveMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error($"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}
				player.LeaveVehicle();
			});


			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.VehicleSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error($"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}
				player.HandleVehicleSync(msg);
			});


			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.PickupObjectMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error($"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}
				player.PickupObject(msg.netId);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.ReleaseObjectMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error($"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}
				player.ReleaseObject(msg.drop);
			});



			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.LightSwitchMessage msg) => {
				Game.Objects.LightSwitch light = Game.LightSwitchManager.Instance.FindLightSwitch(Utils.NetVec3ToGame(msg.pos));
				light.TurnOn(msg.toggle);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.VehicleStateMessage msg) => {
				float startTime = -1;

				NetVehicle vehicle = GetVehicle(msg.vehicleId);
				if (vehicle == null) {
					Logger.Log("Remote player tried to set state of vehicle " + msg.vehicleId + " but there is no vehicle with such id.");
					return;
				}

				if (msg.HasStartTime) {
					startTime = msg.StartTime;
				}

				vehicle.SetEngineState(msg.state, msg.dashstate, startTime);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.VehicleSwitchMessage msg) => {
				float newValueFloat = -1;

				NetVehicle vehicle = GetVehicle(msg.vehicleId);
				if (vehicle == null) {
					Logger.Log("Remote player tried to change a switch in vehicle " + msg.vehicleId + " but there is no vehicle with such id.");
					return;
				}

				if (msg.HasSwitchValueFloat) {
					newValueFloat = msg.SwitchValueFloat;
				}

				vehicle.SetVehicleSwitch(msg.switchID, msg.switchValue, newValueFloat);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.ObjectSyncMessage msg) => {
				ObjectSyncComponent osc;
				ObjectSyncManager.SyncTypes type = (ObjectSyncManager.SyncTypes)msg.SyncType;
				try {
					osc = ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
				}
				catch {
					Logger.Log($"Specified object is not yet added to the ObjectID's Dictionary! (Object ID: {msg.objectID})");
					return;
				}
				if (osc != null) {
					// Set owner.
					if (type == ObjectSyncManager.SyncTypes.SetOwner) {
						if (osc.Owner == ObjectSyncManager.NO_OWNER) {
							osc.OwnerSetToRemote(sender.m_SteamID);
							netManager.GetLocalPlayer().SendObjectSyncResponse(osc.ObjectID, true);
							Logger.Log($"Owner set for object: {osc.transform.name} New owner: {sender.m_SteamID}");
						}
						else {
							Logger.Debug($"Set owner request rejected for object: {osc.transform.name} (Owner: {osc.Owner})");
						}
					}
					// Remove owner.
					else if (type == ObjectSyncManager.SyncTypes.RemoveOwner) {
						if (osc.Owner == sender.m_SteamID) {
							osc.Owner = 0;
						}
					}
					// Force set owner.
					else if (type == ObjectSyncManager.SyncTypes.ForceSetOwner) {
						osc.Owner = sender.m_SteamID;
						netManager.GetLocalPlayer().SendObjectSyncResponse(osc.ObjectID, true);
						osc.SyncTakenByForce();
					}

					// Set object's position and variables
					if (osc.Owner == sender.m_SteamID || type == ObjectSyncManager.SyncTypes.PeriodicSync) {
						if (msg.HasSyncedVariables == true) {
							osc.HandleSyncedVariables(msg.SyncedVariables);
						}
						osc.SetPositionAndRotation(Utils.NetVec3ToGame(msg.position), Utils.NetQuatToGame(msg.rotation));
					}
				}
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.ObjectSyncResponseMessage msg) => {
				ObjectSyncComponent osc = ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
				if (msg.accepted) {
					osc.SyncEnabled = true;
					osc.Owner = Steamworks.SteamUser.GetSteamID().m_SteamID;
				}
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.ObjectSyncRequestMessage msg) => {
				try {
					ObjectSyncComponent osc = ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
					osc.SendObjectSync(ObjectSyncManager.SyncTypes.GenericSync, true);
				}
				catch {
					Logger.Error($"Remote client tried to request object sync of an unknown object, Object ID: {msg.objectID}");
				}
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.EventHookSyncMessage msg) => {
				if (msg.request) {
					EventHook.SendSync(msg.fsmID);
				}
				else {
					if (msg.HasFsmEventName) {
						EventHook.HandleEventSync(msg.fsmID, msg.fsmEventID, msg.FsmEventName);
					}
					else {
						EventHook.HandleEventSync(msg.fsmID, msg.fsmEventID);
					}
				}
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

			Logger.Debug($"Registering vehicle {gameObjectName} (Net ID: {netId})");
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

			if (netManager.IsPlayer || !netManager.IsNetworkPlayerConnected()) {
				return;
			}

			timeToSendPeriodicalUpdate -= Time.deltaTime;

			if (timeToSendPeriodicalUpdate <= 0.0f) {
				var message = new Messages.WorldPeriodicalUpdateMessage();
				message.sunClock = (Byte)Game.GameWorld.Instance.WorldTime;
				message.worldDay = (Byte)Game.GameWorld.Instance.WorldDay;
				Game.GameWeatherManager.Instance.WriteWeather(message.currentWeather);
				netManager.BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);

				timeToSendPeriodicalUpdate = PERIODICAL_UPDATE_INTERVAL;
			}

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

			var pickupables = Game.GamePickupableDatabase.Instance.Pickupables;
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

			Logger.Debug("Writing full world synchronization message.");
			var watch = System.Diagnostics.Stopwatch.StartNew();

			// Write time

			Game.GameWorld gameWorld = Game.GameWorld.Instance;
			msg.dayTime = gameWorld.WorldTime;
			msg.day = gameWorld.WorldDay;

			// Write mailbox name

			msg.mailboxName = gameWorld.PlayerLastName;

			// Write doors

			List<Game.Objects.GameDoor> doors = Game.GameDoorsManager.Instance.doors;
			int doorsCount = doors.Count;
			msg.doors = new Messages.DoorsInitMessage[doorsCount];

			Logger.Debug($"Writing state of {doorsCount} doors.");
			for (int i = 0; i < doorsCount; ++i) {
				var doorMsg = new Messages.DoorsInitMessage();
				Game.Objects.GameDoor door = doors[i];
				doorMsg.position = Utils.GameVec3ToNet(door.Position);
				doorMsg.open = door.IsOpen;
				msg.doors[i] = doorMsg;
			}

			// Write light switches.


			List<Game.Objects.LightSwitch> lights = Game.LightSwitchManager.Instance.lightSwitches;
			int lightCount = lights.Count;
			msg.lights = new Messages.LightSwitchMessage[lightCount];

			Logger.Debug($"Writing light switches state of {lightCount}");
			for (int i = 0; i < lightCount; i++) {
				var lightMsg = new Messages.LightSwitchMessage();
				Game.Objects.LightSwitch light = lights[i];
				lightMsg.pos = Utils.GameVec3ToNet(light.Position);
				lightMsg.toggle = light.SwitchStatus;
				msg.lights[i] = lightMsg;
			}

			// Write weather

			Game.GameWeatherManager.Instance.WriteWeather(msg.currentWeather);

			// Write vehicles.

			int vehiclesCount = vehicles.Count;
			msg.vehicles = new Messages.VehicleInitMessage[vehiclesCount];

			Logger.Debug($"Writing state of {vehiclesCount} vehicles");
			for (int i = 0; i < vehiclesCount; ++i) {
				var vehicleMsg = new Messages.VehicleInitMessage();
				NetVehicle vehicle = vehicles[i];
				vehicleMsg.id = vehicle.NetId;
				vehicleMsg.transform.position = Utils.GameVec3ToNet(vehicle.GetPosition());
				vehicleMsg.transform.rotation = Utils.GameQuatToNet(vehicle.GetRotation());
				msg.vehicles[i] = vehicleMsg;
			}

			// Write pickupables.

			var pickupableMessages = new List<Messages.PickupableSpawnMessage>();
			Logger.Debug($"Writing state of {netPickupables.Count} pickupables");
			foreach (var kv in netPickupables) {
				NetPickupable pickupable = kv.Value;
				if (pickupable.gameObject == null) {
					Logger.Debug($"Null ptr of the pickupable game object {pickupable.NetId}");
					continue;
				}

				var pickupableMsg = new Messages.PickupableSpawnMessage();
				pickupableMsg.id = pickupable.NetId;

				var metaData = pickupable.gameObject.GetComponent<Game.Components.PickupableMetaDataComponent>();
				Client.Assert(metaData != null && metaData.PrefabDescriptor != null, $"Pickupable with broken meta data -- {pickupable.gameObject.name}.");

				pickupableMsg.prefabId = metaData.prefabId;

				Transform transform = pickupable.gameObject.transform;
				pickupableMsg.transform.position = Utils.GameVec3ToNet(transform.position);
				pickupableMsg.transform.rotation = Utils.GameQuatToNet(transform.rotation);

				pickupableMsg.active = pickupable.gameObject.activeSelf;

				List<float> data = new List<float>();

				// Beercases
				if (metaData.PrefabDescriptor.type == Game.GamePickupableDatabase.PrefabType.BeerCase) {
					Game.Objects.BeerCase beer = Game.BeerCaseManager.Instance.FindBeerCase(pickupable.gameObject);
					data.Add(Game.BeerCaseManager.Instance.FullCaseBottles - beer.UsedBottles);
				}

				if (data.Count != 0) {
					pickupableMsg.Data = data.ToArray();
				}
				pickupableMessages.Add(pickupableMsg);
			}

			msg.pickupables = pickupableMessages.ToArray();

			netManager.GetLocalPlayer().WriteSpawnState(msg);

			watch.Stop();
			Logger.Debug("World state has been written. Took " + watch.ElapsedMilliseconds + "ms");
		}


		/// <summary>
		/// Handle full world sync message.
		/// </summary>
		/// <param name="msg">The message to handle.</param>

		public void HandleFullWorldSync(Messages.FullWorldSyncMessage msg) {

			Logger.Debug("Handling full world synchronization message.");
			var watch = System.Diagnostics.Stopwatch.StartNew();

			// Read time

			Game.GameWorld gameWorld = Game.GameWorld.Instance;
			gameWorld.WorldTime = msg.dayTime;
			gameWorld.WorldDay = msg.day;

			// Read mailbox name

			gameWorld.PlayerLastName = msg.mailboxName;

			// Doors.

			foreach (Messages.DoorsInitMessage door in msg.doors) {
				Vector3 position = Utils.NetVec3ToGame(door.position);
				Game.Objects.GameDoor doors = Game.GameDoorsManager.Instance.FindGameDoors(position);
				Client.Assert(doors != null, $"Unable to find doors at: {position}.");
				if (doors.IsOpen != door.open) {
					doors.Open(door.open);
				}
			}

			// Lights.

			foreach (Messages.LightSwitchMessage light in msg.lights) {
				Vector3 position = Utils.NetVec3ToGame(light.pos);
				Game.Objects.LightSwitch lights = Game.LightSwitchManager.Instance.FindLightSwitch(position);
				Client.Assert(lights != null, $"Unable to find light switch at: {position}.");
				if (lights.SwitchStatus != light.toggle) {
					lights.TurnOn(light.toggle);
				}
			}

			// Weather.

			Game.GameWeatherManager.Instance.SetWeather(msg.currentWeather);

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
				if (gameObject && !gameObject.activeSelf) {
					continue;
				}

				DestroyPickupableLocal(id);
			}

			watch.Stop();
			Logger.Debug("Full world synchronization message has been handled. Took " + watch.ElapsedMilliseconds + "ms");
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
			// noop
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

				Logger.Debug($"Handle pickupable destroy {pickupable.name}");
				netPickupables.Remove(netPickupable.NetId);
			}
			else {
				Logger.Debug($"Unhandled pickupable has been destroyed {pickupable.name}");
				Logger.Debug(Environment.StackTrace);
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
				Game.GamePickupableDatabase.PrefabDesc desc = Game.GamePickupableDatabase.Instance.GetPickupablePrefab(msg.prefabId);
				if (gameObject != null) {
					var metaData = gameObject.GetComponent<Game.Components.PickupableMetaDataComponent>();
					if (msg.prefabId == metaData.prefabId) {
						gameObject.SetActive(msg.active);
						gameObject.transform.position = position;
						gameObject.transform.rotation = rotation;
						if (msg.HasData) {
							HandlePickupablesSpawnData(gameObject, desc.type, msg.Data);
						}
						return;
					}
				}

				DestroyPickupableLocal(msg.id);
			}

			GameObject pickupable = Game.GameWorld.Instance.SpawnPickupable(msg.prefabId, position, rotation);
			if (msg.HasData) {
				Game.GamePickupableDatabase.PrefabDesc desc = Game.GamePickupableDatabase.Instance.GetPickupablePrefab(msg.prefabId);
				HandlePickupablesSpawnData(pickupable, desc.type, msg.Data);
			}
			RegisterPickupable(msg.id, pickupable, true);
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
			if (gameObject != null) {
				GameObject.Destroy(gameObject);
			}
			netPickupables.Remove(id);
		}

		/// <summary>
		/// Register pickupable into the network world.
		/// </summary>
		/// <param name="netId">The network id of the pickupable.</param>
		/// <param name="pickupable">The game object representing pickupable.</param>
		/// <param name="remote">Is this remote pickupable?</param>
		public void RegisterPickupable(ushort netId, GameObject pickupable, bool remote = false) {
			Client.Assert(!netPickupables.ContainsKey(netId), $"Duplicate net id {netId}");
			var metaData = pickupable.GetComponent<Game.Components.PickupableMetaDataComponent>();
			Client.Assert(metaData != null, $"Failed to register pickupable. No meta data found. {pickupable.name} ({pickupable.GetInstanceID()})");

			Logger.Debug($"Registering pickupable {pickupable.name} (net id: {netId}, instance id: {pickupable.GetInstanceID()})");

			netPickupables.Add(netId, new NetPickupable(netId, pickupable));

			if (remote) {
				if (metaData.PrefabDescriptor.type == Game.GamePickupableDatabase.PrefabType.BeerCase) {
					Game.BeerCaseManager.Instance.AddBeerCase(pickupable);
				}
			}
		}

		/// <summary>
		/// Set data of pickupables.
		/// </summary>
		/// <param name="pickupable">Pickupable GameObject</param>
		/// <param name="prefabId">Pickupable PrefabID</param>
		/// <param name="data">Pickupable Data</param>
		private void HandlePickupablesSpawnData(GameObject pickupable, Game.GamePickupableDatabase.PrefabType type, float[] data) {
			//Beercase
			if (type == Game.GamePickupableDatabase.PrefabType.BeerCase) {
				Game.BeerCaseManager.Instance.SetBottleCount(pickupable, (int)data[0]);
			}
		}
	}
}
