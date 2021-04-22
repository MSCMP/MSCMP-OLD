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
		/// Net manager owning this world.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// Interval between each periodical update in seconds.
		/// </summary>
		const float PERIODICAL_UPDATE_INTERVAL = 10.0f;

		/// <summary>
		/// Time left to send periodical message.
		/// </summary>
		float timeToSendPeriodicalUpdate = 0.0f;

		/// <summary>
		/// If the player is still handling FullWorldSync.
		/// </summary>
		public bool playerIsLoading = true;

		/// <summary>
		/// Instance.
		/// </summary>
		public static NetWorld Instance;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="netManager">Network manager owning this network world.</param>
		public NetWorld(NetManager netManager) {
			this.netManager = netManager;
			Instance = this;

			GameCallbacks.onWorldUnload += () => { OnGameWorldUnload(); };

			GameCallbacks.onWorldLoad += () => { OnGameWorldLoad(); };

			GameCallbacks.onPlayMakerObjectCreate += (GameObject instance,
					GameObject prefab) => {
				if (!Game.GamePickupableDatabase.IsPickupable(instance)) { return; }

				var metaData =
						prefab.GetComponent<Game.Components.PickupableMetaDataComponent>();
				Client.Assert(metaData != null,
						"Tried to spawn pickupable that has no meta data assigned.");
				RegisterPickupable(instance);

				Messages.PickupableSpawnMessage msg = new Messages.PickupableSpawnMessage();
				msg.prefabId = metaData.prefabId;
				msg.transform.position = Utils.GameVec3ToNet(instance.transform.position);
				msg.transform.rotation = Utils.GameQuatToNet(instance.transform.rotation);
				msg.active = instance.activeSelf;

				// Check for multiple sync components from prefab
				ObjectSyncComponent oscOld = prefab.GetComponent<ObjectSyncComponent>();
				if (instance.GetComponents<ObjectSyncComponent>().Length > 1) {
					foreach (ObjectSyncComponent osc in instance
											 .GetComponents<ObjectSyncComponent>()) {
						if (osc.ObjectID == oscOld.ObjectID) {
							GameObject.Destroy(osc);
						} else {
							msg.id = osc.ObjectID;
						}
					}
				} else {
					msg.id = instance.GetComponent<ObjectSyncComponent>().ObjectID;
				}

				// Determine if object should be spawned on remote client.
				// (Helps to avoid duplicate objects spawning)
				bool sendToRemote = false;
				if (NetManager.Instance.IsHost) {
					Logger.Debug("Sending new object data to client!");
					sendToRemote = true;
				} else {
					if (instance.name.StartsWith("BottleBeerFly")) { sendToRemote = true; }
				}

				if (sendToRemote) {
					netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
					Logger.Debug("Sending new object data to client!");
				}
			};

			GameCallbacks.onPlayMakerObjectActivate += (GameObject instance,
					bool activate) => {
				if (playerIsLoading) { return; }

				if (activate == instance.activeSelf) { return; }

				if (!GamePickupableDatabase.IsPickupable(instance)) { return; }

				ObjectSyncComponent pickupable = GetPickupableByGameObject(instance);
				if (pickupable == null) { return; }

				if (activate) {
					var metaData =
							pickupable.gameObject.GetComponent<PickupableMetaDataComponent>();

					Messages.PickupableSpawnMessage msg =
							new Messages.PickupableSpawnMessage();
					msg.id = pickupable.ObjectID;
					msg.prefabId = metaData.prefabId;
					msg.transform.position = Utils.GameVec3ToNet(instance.transform.position);
					msg.transform.rotation = Utils.GameQuatToNet(instance.transform.rotation);
					netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
				} else {
					Messages.PickupableActivateMessage msg =
							new Messages.PickupableActivateMessage();
					msg.id = pickupable.ObjectID;
					msg.activate = false;
					netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
				}
			};

			GameCallbacks.onPlayMakerObjectDestroy += (GameObject instance) => {
				if (!Game.GamePickupableDatabase.IsPickupable(instance)) { return; }

				ObjectSyncComponent pickupable = GetPickupableByGameObject(instance);
				if (pickupable == null) {
					Logger.Debug(
							$"Pickupable {instance.name} has been destroyed however it is not registered, skipping removal.");
					return;
				}

				HandlePickupableDestroy(instance);
			};

			GameCallbacks.onPlayMakerSetPosition +=
					(GameObject gameObject, Vector3 position, Space space) => {
						if (!Game.GamePickupableDatabase.IsPickupable(gameObject)) { return; }

						ObjectSyncComponent pickupable = GetPickupableByGameObject(gameObject);
						if (pickupable == null) { return; }

						if (space == Space.Self) { position += gameObject.transform.position; }

						Messages.PickupableSetPositionMessage msg =
								new Messages.PickupableSetPositionMessage();
						msg.id = pickupable.ObjectID;
						msg.position = Utils.GameVec3ToNet(position);
						netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);
					};

			RegisterNetworkMessagesHandlers(netManager.MessageHandler);
		}

		/// <summary>
		/// Register world related network message handlers.
		/// </summary>
		/// <param name="netMessageHandler">The network message handler to register
		/// messages to.</param>
		void RegisterNetworkMessagesHandlers(NetMessageHandler netMessageHandler) {
			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender,
							Messages.PickupableSetPositionMessage msg) => {
						Client.Assert(ObjectSyncManager.Instance.ObjectIDs.ContainsKey(msg.id),
								$"Tried to move pickupable that is not spawned {msg.id}.");
						GameObject gameObject =
								ObjectSyncManager.Instance.ObjectIDs[msg.id].gameObject;
						gameObject.transform.position = Utils.NetVec3ToGame(msg.position);
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.PickupableActivateMessage msg) => {
						GameObject gameObject = null;
						if (ObjectSyncManager.Instance.ObjectIDs.ContainsKey(msg.id)) {
							gameObject = ObjectSyncManager.Instance.ObjectIDs[msg.id].gameObject;
						}
						Client.Assert(gameObject != null,
								"Tried to activate pickupable but its not spawned!");

						if (msg.activate) {
							gameObject.SetActive(true);
						} else {
							if (gameObject != null) { gameObject.SetActive(false); }
						}
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.PickupableSpawnMessage msg) => {
						SpawnPickupable(msg);
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.PickupableDestroyMessage msg) => {
						if (!ObjectSyncManager.Instance.ObjectIDs.ContainsKey(msg.id)) {
							return;
						}

						GameObject go;
						try {
							go = ObjectSyncManager.Instance.ObjectIDs[msg.id].gameObject;
						} catch {
							Logger.Error(
									"Failed to remove object: OSC found but can't get GameObject.");
							return;
						}

						GameObject.Destroy(
								ObjectSyncManager.Instance.ObjectIDs[msg.id].gameObject);
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender,
							Messages.WorldPeriodicalUpdateMessage msg) => {
						// Game reports 'next hour' - we want to have transition so correct it.
						GameWorld.Instance.WorldTime = (float)msg.sunClock - 2.0f;
						GameWorld.Instance.WorldDay = (int)msg.worldDay;
						GameWeatherManager.Instance.SetWeather(msg.currentWeather);
					});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.PlayerSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Log(
							$"Received synchronization packet from {sender} but there is not player registered using this id.");
					return;
				}

				player.HandleSynchronize(msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.AnimSyncMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Log(
							$"Received animation synchronization packet from {sender} but there is not player registered using this id.");
					return;
				}

				player.HandleAnimSynchronize(msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.OpenDoorsMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Log(
							$"Received OpenDoorsMessage however there is no matching player {sender}! (open: {msg.open}");
					return;
				}

				GameDoor doors = GameDoorsManager.Instance.FindGameDoors(
						Utils.NetVec3ToGame(msg.position));
				if (doors == null) {
					Logger.Log(
							$"Player tried to open door, however, the door could not be found!");
					return;
				}
				doors.Open(msg.open);
			});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.FullWorldSyncMessage msg) => {
						NetPlayer player = netManager.GetPlayer(sender);

						// This one should never happen - if happens there is something done
						// miserably wrong.
						Client.Assert(player != null,
								$"There is no player matching given steam id {sender}.");

						// Handle full world state synchronization.

						HandleFullWorldSync(msg);

						// Spawn host character.

						player.Spawn();

						// Set player state.

						player.Teleport(Utils.NetVec3ToGame(msg.spawnPosition),
								Utils.NetQuatToGame(msg.spawnRotation));

						if (msg.pickedUpObject != NetPickupable.INVALID_ID) {
							player.PickupObject(msg.pickedUpObject);
						}

						// World is loaded! Notify network manager about that.

						netManager.OnNetworkWorldLoaded();
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.AskForWorldStateMessage msg) => {
						var msgF = new Messages.FullWorldSyncMessage();
						WriteFullWorldSync(msgF);
						netManager.BroadcastMessage(
								msgF, Steamworks.EP2PSend.k_EP2PSendReliable);
					});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.VehicleEnterMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error(
							$"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}

				ObjectSyncComponent vehicle =
						ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
				if (vehicle == null) {
					Logger.Error("Player " + player.SteamId +
							" tried to enter vehicle with Object ID " + msg.objectID +
							" but there is no vehicle with such id.");
					return;
				}

				player.EnterVehicle(vehicle, msg.passenger);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.VehicleLeaveMessage msg) => {
				NetPlayer player = netManager.GetPlayer(sender);
				if (player == null) {
					Logger.Error(
							$"Steam user of id {sender} send message however there is no active player matching this id.");
					return;
				}
				player.LeaveVehicle();
			});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.VehicleStateMessage msg) => {
						float startTime = -1;

						ObjectSyncComponent vehicle =
								ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
						if (vehicle == null) {
							Logger.Log("Remote player tried to set state of vehicle " +
									msg.objectID + " but there is no vehicle with such id.");
							return;
						}

						if (msg.HasStartTime) { startTime = msg.StartTime; }

						PlayerVehicle subType = vehicle.GetObjectSubtype() as PlayerVehicle;
						subType.SetEngineState((PlayerVehicle.EngineStates)msg.state,
								(PlayerVehicle.DashboardStates)msg.dashstate, startTime);
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.VehicleSwitchMessage msg) => {
						float newValueFloat = -1;

						PlayerVehicle vehicle =
								ObjectSyncManager.Instance.ObjectIDs[msg.objectID].GetObjectSubtype()
										as PlayerVehicle;
						if (vehicle == null) {
							Logger.Log("Remote player tried to change a switch in vehicle " +
									msg.objectID + " but there is no vehicle with such id.");
							return;
						}

						if (msg.HasSwitchValueFloat) { newValueFloat = msg.SwitchValueFloat; }

						vehicle.SetVehicleSwitch((PlayerVehicle.SwitchIDs)msg.switchID,
								msg.switchValue, newValueFloat);
					});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.LightSwitchMessage msg) => {
						LightSwitch light = Game.LightSwitchManager.Instance.FindLightSwitch(
								Utils.NetVec3ToGame(msg.pos));
						light.TurnOn(msg.toggle);
					});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.ObjectSyncMessage msg) => {
				ObjectSyncComponent osc;
				ObjectSyncManager.SyncTypes type = (ObjectSyncManager.SyncTypes)msg.SyncType;
				try {
					osc = ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
				} catch {
					Logger.Log(
							$"Specified object is not yet added to the ObjectID's Dictionary! (Object ID: {msg.objectID})");
					return;
				}
				if (osc != null) {
					// Set owner.
					if (type == ObjectSyncManager.SyncTypes.SetOwner) {
						if (osc.Owner == ObjectSyncManager.NO_OWNER ||
								osc.Owner == sender.m_SteamID) {
							osc.OwnerSetToRemote(sender.m_SteamID);
							netManager.GetLocalPlayer().SendObjectSyncResponse(osc.ObjectID, true);
						} else {
							Logger.Debug(
									$"Set owner request rejected for object: {osc.transform.name} (Owner: {osc.Owner} Sender: {sender.m_SteamID})");
						}
					}
					// Remove owner.
					else if (type == ObjectSyncManager.SyncTypes.RemoveOwner) {
						if (osc.Owner == sender.m_SteamID) { osc.OwnerRemoved(); }
					}
					// Force set owner.
					else if (type == ObjectSyncManager.SyncTypes.ForceSetOwner) {
						osc.Owner = sender.m_SteamID;
						netManager.GetLocalPlayer().SendObjectSyncResponse(osc.ObjectID, true);
						osc.SyncTakenByForce();
						osc.SyncEnabled = false;
					}

					// Set object's position and variables
					if (osc.Owner == sender.m_SteamID ||
							type == ObjectSyncManager.SyncTypes.PeriodicSync) {
						if (msg.HasSyncedVariables == true) {
							osc.HandleSyncedVariables(msg.SyncedVariables);
						}
						osc.SetPositionAndRotation(Utils.NetVec3ToGame(msg.position),
								Utils.NetQuatToGame(msg.rotation));
					}
				}
			});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.ObjectSyncResponseMessage msg) => {
						ObjectSyncComponent osc =
								ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
						if (msg.accepted) {
							osc.SyncEnabled = true;
							osc.Owner = Steamworks.SteamUser.GetSteamID().m_SteamID;
						}
					});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender,
																							 Messages.ObjectSyncRequestMessage
																									 msg) => {
				try {
					ObjectSyncComponent osc =
							ObjectSyncManager.Instance.ObjectIDs[msg.objectID];
					osc.SendObjectSync(ObjectSyncManager.SyncTypes.GenericSync, true, true);
				} catch {
					Logger.Error(
							$"Remote client tried to request object sync of an unknown object, Object ID: {msg.objectID}");
				}
			});

			netMessageHandler.BindMessageHandler(
					(Steamworks.CSteamID sender, Messages.EventHookSyncMessage msg) => {
						if (msg.request) {
							EventHook.SendSync(msg.fsmID);
						} else {
							if (msg.HasFsmEventName) {
								EventHook.HandleEventSync(
										msg.fsmID, msg.fsmEventID, msg.FsmEventName);
							} else {
								EventHook.HandleEventSync(msg.fsmID, msg.fsmEventID);
							}
						}
					});
		}

		/// <summary>
		/// Update net world.
		/// </summary>
		public void Update() {

			if (netManager.IsPlayer || !netManager.IsNetworkPlayerConnected()) { return; }

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
			// foreach (var v in vehicles) {
			//	v.FixedUpdate();
			//}
			// May need this later.
		}

		/// <summary>
		/// Called when game world gets loaded.
		/// </summary>
		public void OnGameWorldLoad() {}

		/// <summary>
		/// Called when game world gets unloaded.
		/// </summary>
		private void OnGameWorldUnload() {
			ObjectSyncManager.Instance.ObjectIDs.Clear();
		}

		/// <summary>
		/// Write full world synchronization message.
		/// </summary>
		/// <param name="msg">The message to write to.</param>
		public void WriteFullWorldSync(Messages.FullWorldSyncMessage msg) {
			Logger.Debug("Writing full world synchronization message.");
			var watch = System.Diagnostics.Stopwatch.StartNew();

			// 'Player is loading' is only applicable for remote client.
			playerIsLoading = false;

			// Write time

			Game.GameWorld gameWorld = Game.GameWorld.Instance;
			msg.dayTime = gameWorld.WorldTime;
			msg.day = gameWorld.WorldDay;

			// Write mailbox name

			msg.mailboxName = gameWorld.PlayerLastName;

			// Write doors

			List<GameDoor> doors = GameDoorsManager.Instance.doors;
			int doorsCount = doors.Count;
			msg.doors = new Messages.DoorsInitMessage[doorsCount];

			Logger.Debug($"Writing state of {doorsCount} doors.");
			for (int i = 0; i < doorsCount; ++i) {
				var doorMsg = new Messages.DoorsInitMessage();
				GameDoor door = doors[i];
				doorMsg.position = Utils.GameVec3ToNet(door.Position);
				doorMsg.open = door.IsOpen;
				msg.doors[i] = doorMsg;
			}

			// Write light switches.

			List<LightSwitch> lights = Game.LightSwitchManager.Instance.lightSwitches;
			int lightCount = lights.Count;
			msg.lights = new Messages.LightSwitchMessage[lightCount];

			Logger.Debug($"Writing light switches state of {lightCount}");
			for (int i = 0; i < lightCount; i++) {
				var lightMsg = new Messages.LightSwitchMessage();
				LightSwitch light = lights[i];
				lightMsg.pos = Utils.GameVec3ToNet(light.Position);
				lightMsg.toggle = light.SwitchStatus;
				msg.lights[i] = lightMsg;
			}

			// Write weather

			GameWeatherManager.Instance.WriteWeather(msg.currentWeather);

			// Write objects. (Pickupables, Player vehicles, AI vehicles)

			var pickupableMessages = new List<Messages.PickupableSpawnMessage>();
			Logger.Debug(
					$"Writing state of {ObjectSyncManager.Instance.ObjectIDs.Count} objects");
			foreach (var kv in ObjectSyncManager.Instance.ObjectIDs) {
				ObjectSyncComponent osc = kv.Value;
				if (osc == null) { continue; }
				if (osc.ObjectType != ObjectSyncManager.ObjectTypes.Pickupable) { continue; }
				bool wasActive = true;
				if (!osc.gameObject.activeSelf) {
					wasActive = false;
					osc.gameObject.SetActive(true);
				}
				Logger.Log($"Writing object: {osc.gameObject.name}");

				var pickupableMsg = new Messages.PickupableSpawnMessage();

				var metaData = osc.gameObject.GetComponent<PickupableMetaDataComponent>();
				Client.Assert(metaData != null && metaData.PrefabDescriptor != null,
						$"Object with broken meta data -- {osc.gameObject.name}.");

				pickupableMsg.prefabId = metaData.prefabId;

				Transform transform = osc.gameObject.transform;
				pickupableMsg.transform.position = Utils.GameVec3ToNet(transform.position);
				pickupableMsg.transform.rotation = Utils.GameQuatToNet(transform.rotation);

				pickupableMsg.active = osc.gameObject.activeSelf;

				// ObjectID
				pickupableMsg.id =
						osc.gameObject.GetComponent<ObjectSyncComponent>().ObjectID;

				List<float> data = new List<float>();

				if (data.Count != 0) { pickupableMsg.Data = data.ToArray(); }
				if (!wasActive) { osc.gameObject.SetActive(false); }
				pickupableMessages.Add(pickupableMsg);
			}

			msg.pickupables = pickupableMessages.ToArray();

			netManager.GetLocalPlayer().WriteSpawnState(msg);

			watch.Stop();
			Logger.Debug(
					"World state has been written. Took " + watch.ElapsedMilliseconds + "ms");
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
				Game.Objects.GameDoor doors =
						Game.GameDoorsManager.Instance.FindGameDoors(position);
				Client.Assert(doors != null, $"Unable to find doors at: {position}.");
				if (doors.IsOpen != door.open) { doors.Open(door.open); }
			}

			// Lights.

			foreach (Messages.LightSwitchMessage light in msg.lights) {
				Vector3 position = Utils.NetVec3ToGame(light.pos);
				Game.Objects.LightSwitch lights =
						Game.LightSwitchManager.Instance.FindLightSwitch(position);
				Client.Assert(
						lights != null, $"Unable to find light switch at: {position}.");
				if (lights.SwitchStatus != light.toggle) { lights.TurnOn(light.toggle); }
			}

			// Weather.

			GameWeatherManager.Instance.SetWeather(msg.currentWeather);

			// Pickupables

			foreach (Messages.PickupableSpawnMessage pickupableMsg in msg.pickupables) {
				SpawnPickupable(pickupableMsg);
			}

			// Remove spawned (and active) pickupables that we did not get info about.

			foreach (var kv in GamePickupableDatabase.Instance.Pickupables) {
				if (kv.Value.GetComponent<ObjectSyncComponent>() == null) {
					GameObject.Destroy(kv.Value);
				}
			}

			GamePickupableDatabase.Instance.Pickupables.Clear();
			playerIsLoading = false;

			watch.Stop();
			Logger.Debug("Full world synchronization message has been handled. Took " +
					watch.ElapsedMilliseconds + "ms");
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
		/// Get pickupable game object from object id.
		/// </summary>
		/// <param name="objectID">Object id of the pickupable.</param>
		/// <returns>Game object representing the given pickupable or null if there is no
		/// pickupable matching this network id.</returns>
		public GameObject GetPickupableGameObject(int objectID) {
			if (ObjectSyncManager.Instance.ObjectIDs.ContainsKey(objectID)) {
				return ObjectSyncManager.Instance.ObjectIDs[objectID].gameObject;
			}
			return null;
		}

		/// <summary>
		/// Get pickupable object id from game object.
		/// </summary>
		/// <param name="go">Game object to get object id for.</param>
		/// <returns>The object id of pickupable or invalid id of the pickupable if no
		/// object ID is found for given game object.</returns>
		public ObjectSyncComponent GetPickupableByGameObject(GameObject go) {
			foreach (var osc in ObjectSyncManager.Instance.ObjectIDs) {
				try {
					if (osc.Value.gameObject == go) { return osc.Value; }
				} catch {}
			}
			Logger.Error("GetPickupableByGameObject: Couldn't find GameObject!");
			return null;
		}

		/// <summary>
		/// Get pickupable object ID from game object.
		/// </summary>
		/// <param name="go">Game object to get object ID for.</param>
		/// <returns>The object ID of pickupable or invalid ID of the pickupable if no
		/// object ID is found for given game object.</returns>
		public int GetPickupableObjectId(GameObject go) {
			foreach (var osc in ObjectSyncManager.Instance.ObjectIDs) {
				if (osc.Value.gameObject == go) { return osc.Value.ObjectID; }
			}
			return NetPickupable.INVALID_ID;
		}

		/// <summary>
		/// Handle destroy of pickupable game object.
		/// </summary>
		/// <param name="pickupable">The destroyed pickupable.</param>
		public void HandlePickupableDestroy(GameObject pickupable) {
			ObjectSyncComponent osc = GetPickupableByGameObject(pickupable);
			if (osc != null) {
				Messages.PickupableDestroyMessage msg =
						new Messages.PickupableDestroyMessage();
				msg.id = osc.ObjectID;
				netManager.BroadcastMessage(msg, Steamworks.EP2PSend.k_EP2PSendReliable);

				Logger.Debug(
						$"Handle pickupable destroy {pickupable.name}, Object ID: {osc.ObjectID}");
			} else {
				Logger.Debug($"Unhandled pickupable has been destroyed {pickupable.name}");
				Logger.Debug(Environment.StackTrace);
			}
		}

		/// <summary>
		/// Spawn pickupable from network message.
		/// </summary>
		/// <param name="msg">The message containing info about pickupable to
		/// spawn.</param>
		public void SpawnPickupable(Messages.PickupableSpawnMessage msg) {
			Vector3 position = Utils.NetVec3ToGame(msg.transform.position);
			Quaternion rotation = Utils.NetQuatToGame(msg.transform.rotation);

			if (ObjectSyncManager.Instance.ObjectIDs.ContainsKey(msg.id)) {
				ObjectSyncComponent osc = ObjectSyncManager.Instance.ObjectIDs[msg.id];
				// Ignore spawn requests for items that are already spawned.
				if (osc.ObjectID == msg.id) { return; }
				GameObject gameObject = osc.gameObject;
				GamePickupableDatabase.PrefabDesc desc =
						GamePickupableDatabase.Instance.GetPickupablePrefab(msg.prefabId);
				if (gameObject != null) {
					var metaData = gameObject.GetComponent<PickupableMetaDataComponent>();
					// Incorrect prefab found.
					if (msg.prefabId != metaData.prefabId) {
						bool resolved = false;
						foreach (var go in ObjectSyncManager.Instance.ObjectIDs) {
							if (go.Value.gameObject.GetComponent<PickupableMetaDataComponent>()
											.prefabId == msg.prefabId) {
								gameObject = go.Value.gameObject;
								Logger.Log("Prefab mismatch was resolved.");
								resolved = true;
								break;
							}
						}
						if (!resolved) {
							Client.Assert(true, "Prefab ID mismatch couldn't be resolved!");
						}
					}
					gameObject.SetActive(msg.active);
					gameObject.transform.position = position;
					gameObject.transform.rotation = rotation;

					if (gameObject.GetComponent<ObjectSyncComponent>() != null) {
						GameObject.Destroy(gameObject.GetComponent<ObjectSyncComponent>());
					}
					gameObject.AddComponent<ObjectSyncComponent>().Setup(
							ObjectSyncManager.ObjectTypes.Pickupable, msg.id);
					return;
				}

				DestroyPickupableLocal(msg.id);
			}

			GameObject pickupable = GameWorld.Instance.SpawnPickupable(
					msg.prefabId, position, rotation, msg.id);
			RegisterPickupable(pickupable, true);
			if (pickupable.GetComponent<ObjectSyncComponent>() != null) {
				GameObject.Destroy(pickupable.GetComponent<ObjectSyncComponent>());
			}
			pickupable.AddComponent<ObjectSyncComponent>().Setup(
					ObjectSyncManager.ObjectTypes.Pickupable, msg.id);
		}

		/// <summary>
		/// Destroy given pickupable from the game without sending destroy message to
		/// players.
		/// </summary>
		/// <param name="id">The object ID of the pickupable to destroy.</param>
		private void DestroyPickupableLocal(int id) {
			if (!ObjectSyncManager.Instance.ObjectIDs.ContainsKey(id)) { return; }
			var gameObject = ObjectSyncManager.Instance.ObjectIDs[id].gameObject;
			if (gameObject != null) { GameObject.Destroy(gameObject); }
		}

		/// <summary>
		/// Register pickupable into the network world. (Deprecated)
		/// </summary>
		/// <param name="pickupable">The game object representing pickupable.</param>
		/// <param name="remote">Is this remote pickupable?</param>
		public void RegisterPickupable(GameObject pickupable, bool remote = false) {
			var metaData = pickupable.GetComponent<PickupableMetaDataComponent>();
			Client.Assert(metaData != null,
					$"Failed to register pickupable. No meta data found. {pickupable.name} ({pickupable.GetInstanceID()})");

			Logger.Debug(
					$"Registering pickupable {pickupable.name} (instance id: {pickupable.GetInstanceID()})");
		}
	}
}
