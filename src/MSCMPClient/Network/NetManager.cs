using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MSCMP.UI;

namespace MSCMP.Network {
	class NetManager {
		private const int MAX_PLAYERS = 2;
		private Steamworks.Callback<Steamworks.GameLobbyJoinRequested_t> gameLobbyJoinRequestedCallback = null;
		private Steamworks.Callback<Steamworks.P2PSessionRequest_t> p2pSessionRequestCallback = null;
		private Steamworks.CallResult<Steamworks.LobbyCreated_t> lobbyCreatedCallResult = null;
		private Steamworks.CallResult<Steamworks.LobbyEnter_t> lobbyEnterCallResult = null;
		public enum Mode {
			None,
			Host,
			Player
		}

		public enum State {
			Idle,
			CreatingLobby,
			LoadingGameWorld,
			Playing
		}

		private State state = State.Idle;
		private Mode mode = Mode.None;

		public bool IsHost {
			get { return mode == Mode.Host; }
		}
		public bool IsPlayer {
			get { return mode == Mode.Player; }
		}
		public bool IsOnline {
			get { return mode != Mode.None; }
		}
		public bool IsPlaying {
			get { return state == State.Playing;  }
		}
		private Steamworks.CSteamID currentLobbyId = Steamworks.CSteamID.Nil;

		private NetPlayer[] players = new NetPlayer[MAX_PLAYERS];

		/// <summary>
		/// The interval between sending individual heartbeat.
		/// </summary>
		const float HEARTBEAT_INTERVAL = 5.0f;

		/// <summary>
		/// Timeout time of the connection.
		/// </summary>
		const float TIMEOUT_TIME = 20.0f;

		/// <summary>
		/// How many seconds left before sending next heartbeat?
		/// </summary>
		float timeToSendHeartbeat = 0.0f;

		/// <summary>
		/// How many seconds passed since last heart beat was received.
		/// </summary>
		float timeSinceLastHeartbeat = 0.0f;

		/// <summary>
		/// The value of the clock on the remote players' computer.
		/// </summary>
		ulong remoteClock = 0;

		/// <summary>
		/// Current ping value.
		/// </summary>
		uint ping = 0;

		/// <summary>
		/// The time when network manager was created in UTC.
		/// </summary>
		DateTime netManagerCreationTime;

		/// <summary>
		/// Network world.
		/// </summary>
		NetWorld netWorld = null;

		/// <summary>
		/// The network message handler.
		/// </summary>
		NetMessageHandler netMessageHandler = null;

		/// <summary>
		/// Get net manager's message handler.
		/// </summary>
		public NetMessageHandler MessageHandler {
			get { return netMessageHandler;  }
		}

		public NetManager() {
			this.netManagerCreationTime = DateTime.UtcNow;
			netMessageHandler = new NetMessageHandler(this);
			netWorld = new NetWorld(this);

			p2pSessionRequestCallback = Steamworks.Callback<Steamworks.P2PSessionRequest_t>.Create((Steamworks.P2PSessionRequest_t result) => {
				if (!Steamworks.SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote)) {
					Logger.Log("Accepted p2p session with " + result.m_steamIDRemote.ToString());
				}
			});

			gameLobbyJoinRequestedCallback = Steamworks.Callback<Steamworks.GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

			lobbyCreatedCallResult = new Steamworks.CallResult<Steamworks.LobbyCreated_t>((Steamworks.LobbyCreated_t result, bool ioFailure) => {
				if (result.m_eResult != Steamworks.EResult.k_EResultOK) {
					Logger.Log("Oh my fucking god i failed to create a lobby for you. Please forgive me. (result: " + result.m_eResult + ")");

					MPGUI.Instance.ShowMessageBox("Failed to create lobby due to steam error.\n" + result.m_eResult, () => {
						MPController.Instance.LoadLevel("MainMenu");
					});
					return;
				}

				Logger.Log("Hey you! I have lobby id for you! " + result.m_ulSteamIDLobby);

				// Setup local player.
				players[0] = new NetLocalPlayer(this, netWorld, Steamworks.SteamUser.GetSteamID());

				mode = Mode.Host;
				state = State.Playing;
				currentLobbyId = new Steamworks.CSteamID(result.m_ulSteamIDLobby);
			});

			lobbyEnterCallResult = new Steamworks.CallResult<Steamworks.LobbyEnter_t>((Steamworks.LobbyEnter_t result, bool ioFailure) => {
				if (result.m_EChatRoomEnterResponse != (uint)Steamworks.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess) {
					Logger.Log("Oh my fucking god i failed to join the lobby for you. Please forgive me. (reponse: " + result.m_EChatRoomEnterResponse + ")");

					players[1] = null;
					return;
				}

				// Setup local player.
				players[0] = new NetLocalPlayer(this, netWorld, Steamworks.SteamUser.GetSteamID());

				Logger.Log("Oh hello! " + result.m_ulSteamIDLobby);

				mode = Mode.Player;
				state = State.LoadingGameWorld;
				currentLobbyId = new Steamworks.CSteamID(result.m_ulSteamIDLobby);

				ShowLoadingScreen(true);
				SendHandshake();
			});

			RegisterProtocolMessagesHandlers();
		}

		/// <summary>
		/// Register protocol related network messages handlers.
		/// </summary>
		void RegisterProtocolMessagesHandlers() {
			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.HandshakeMessage msg) => {
				remoteClock = msg.clock;
				HandleHandshake(sender, msg);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.HeartbeatMessage msg) => {
				var message = new Messages.HeartbeatResponseMessage();
				message.clientClock = msg.clientClock;
				message.clock = GetNetworkClock();
				BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.HeartbeatResponseMessage msg) => {
				ping = (uint)(GetNetworkClock() - msg.clientClock);

				// TODO: Some smart lag compensation.
				remoteClock = msg.clock;

				timeSinceLastHeartbeat = 0.0f;
			});

			netMessageHandler.BindMessageHandler((Steamworks.CSteamID sender, Messages.DisconnectMessage msg) => {
				HandleDisconnect(false);
			});
		}

		/// <summary>
		/// Show loading screen.
		/// </summary>
		/// <param name="show">Show or hide loading screen.</param>
		private void ShowLoadingScreen(bool show) {
			if (Application.loadedLevelName == "MainMenu") {
				// This is not that slow as you may think - seriously!

				GameObject []gos = Resources.FindObjectsOfTypeAll<GameObject>();
				GameObject loadingScreen = null;
				foreach (GameObject go in gos) {
					if (go.transform.parent == null && go.name == "Loading") {
						loadingScreen = go;
						break;
					}
				}
				loadingScreen.SetActive(show);
			}
		}

		/// <summary>
		/// Get network clock with the milliseconds resolution. (time since network manager was created)
		/// </summary>
		/// <returns>Network clock time in miliseconds.</returns>
		public ulong GetNetworkClock() {
			return (ulong)((DateTime.UtcNow - this.netManagerCreationTime).TotalMilliseconds);
		}


		/// <summary>
		/// Broadcasts message to connected players.
		/// </summary>
		/// <typeparam name="T">The type of the message to broadcast.</typeparam>
		/// <param name="message">The message to broadcast.</param>
		/// <param name="sendType">The send type.</param>
		/// <param name="channel">The channel used to deliver message.</param>
		/// <returns></returns>
		public bool BroadcastMessage<T>(T message, Steamworks.EP2PSend sendType, int channel = 0) where T : INetMessage {
			if (players[1] == null) {
				return false;
			}
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);

			writer.Write((byte)message.MessageId);
			if (! message.Write(writer)) {
				Client.FatalError("Failed to write network message " + message.MessageId);
				return false;
			}

			foreach (NetPlayer player in players) {
				if (player is NetLocalPlayer) {
					continue;
				}

				player?.SendPacket(stream.GetBuffer(), sendType, channel);
			}
			return true;
		}

		/// <summary>
		/// Callback called when client accepts lobby join request from other steam user.
		/// </summary>
		/// <param name="request">The request.</param>
		private void OnGameLobbyJoinRequested(Steamworks.GameLobbyJoinRequested_t request) {
			Steamworks.SteamAPICall_t apiCall = Steamworks.SteamMatchmaking.JoinLobby(request.m_steamIDLobby);
			if (apiCall == Steamworks.SteamAPICall_t.Invalid) {
				Logger.Log("Unable to join lobby.");
				return;
			}

			Logger.Log("Setup player.");

			// Setup remote player. The HOST.
			timeSinceLastHeartbeat = 0.0f;
			players[1] = new NetPlayer(this, netWorld, request.m_steamIDFriend);

			lobbyEnterCallResult.Set(apiCall);
		}

		/// <summary>
		/// Setup lobby to host a game.
		/// </summary>
		/// <returns>true if lobby setup request was properly sent, false otherwise</returns>
		public bool SetupLobby() {
			Logger.Log("Setting up lobby.");
			Steamworks.SteamAPICall_t apiCall = Steamworks.SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly, MAX_PLAYERS);
			if (apiCall == Steamworks.SteamAPICall_t.Invalid) {
				Logger.Log("Unable to create lobby.");
				return false;
			}
			Logger.Log("Waiting for lobby create reply..");
			lobbyCreatedCallResult.Set(apiCall);
			return true;
		}

		/// <summary>
		/// Leave current lobby.
		/// </summary>
		private void LeaveLobby() {
			Steamworks.SteamMatchmaking.LeaveLobby(currentLobbyId);
			currentLobbyId = Steamworks.CSteamID.Nil;
			mode = Mode.None;
			state = State.Idle;
			players[0].Dispose();
			players[0] = null;
			Logger.Log("Left lobby.");
		}

		/// <summary>
		/// Invite player with given id to the lobby.
		/// </summary>
		/// <param name="invitee">The steam id of the player to invite.</param>
		/// <returns>true if player was invited, false otherwise</returns>
		public bool InviteToMyLobby(Steamworks.CSteamID invitee) {
			if (!IsHost) {
				return false;
			}
			return Steamworks.SteamMatchmaking.InviteUserToLobby(currentLobbyId, invitee);
		}

		/// <summary>
		/// Is another player connected and playing in the session?
		/// </summary>
		/// <returns>true if there is another player connected and playing in the session, false otherwise</returns>
		public bool IsNetworkPlayerConnected() {
			return players[1] != null;
		}

		/// <summary>
		/// Cleanup remote player.
		/// </summary>
		public void CleanupPlayer() {
			if (players[1] == null) {
				return;
			}
			Steamworks.SteamNetworking.CloseP2PSessionWithUser(players[1].SteamId);
			players[1].Dispose();
			players[1] = null;
		}


		/// <summary>
		/// Disconnect from the active multiplayer session.
		/// </summary>
		public void Disconnect() {
			BroadcastMessage(new Messages.DisconnectMessage(), Steamworks.EP2PSend.k_EP2PSendReliable);
			LeaveLobby();
		}

		/// <summary>
		/// Handle disconnect of the remote player.
		/// </summary>
		/// <param name="timeout">Was the disconnect caused by timeout?</param>
		private void HandleDisconnect(bool timeout) {
			ShowLoadingScreen(false);
			CleanupPlayer();

			// Go to main menu if we are normal player - the session just closed.

			if (IsPlayer) {
				LeaveLobby();
				MPController.Instance.LoadLevel("MainMenu");

				if (timeout) {
					MPGUI.Instance.ShowMessageBox("Session timed out.");
				}
				else {
					MPGUI.Instance.ShowMessageBox("Host closed the session.");
				}
			}
		}

		/// <summary>
		/// Update connection state.
		/// </summary>
		private void UpdateHeartbeat() {
			if (!IsNetworkPlayerConnected()) {
				return;
			}

			timeSinceLastHeartbeat += Time.deltaTime;

			if (timeSinceLastHeartbeat >= TIMEOUT_TIME) {
				HandleDisconnect(true);
			}
			else {
				timeToSendHeartbeat -= Time.deltaTime;
				if (timeToSendHeartbeat <= 0.0f) {
					Messages.HeartbeatMessage message = new Messages.HeartbeatMessage();
					message.clientClock = GetNetworkClock();
					BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);

					timeToSendHeartbeat = HEARTBEAT_INTERVAL;
				}
			}
		}


		/// <summary>
		/// Process incomming network messages.
		/// </summary>
		private void ProcessMessages() {


			uint size = 0;
			while (Steamworks.SteamNetworking.IsP2PPacketAvailable(out size)) {
				if (size == 0) {
					Logger.Log("Received empty p2p packet");
					continue;
				}

				// TODO: Pre allocate this buffer and reuse it here - we don't want garbage collector to go crazy with that.

				byte[] data = new byte[size];

				uint msgSize = 0;
				Steamworks.CSteamID senderSteamId = Steamworks.CSteamID.Nil;
				if (!Steamworks.SteamNetworking.ReadP2PPacket(data, size, out msgSize, out senderSteamId)) {
					continue;
				}

				// TODO: Joining of the messages if are split?

				if (msgSize != size || msgSize == 0) {
					Logger.Log("Invalid packet size");
					continue;
				}

				MemoryStream stream = new MemoryStream(data);
				BinaryReader reader = new BinaryReader(stream);

				byte messageId = reader.ReadByte();
				netMessageHandler.ProcessMessage(messageId, senderSteamId, reader);
			}
		}

		/// <summary>
		/// Fixed update of the network.
		/// </summary>
		public void FixedUpdate() {
			netWorld.FixedUpdate();
		}

		/// <summary>
		/// Update network manager state.
		/// </summary>
		public void Update() {
			if (!IsOnline) {
				return;
			}

			netWorld.Update();
			UpdateHeartbeat();
			ProcessMessages();

#if !PUBLIC_RELEASE
			if (Input.GetKeyDown(KeyCode.F8) && players[1] != null) {
				var localPlayer = GetLocalPlayer();
				localPlayer.Teleport(players[1].GetPosition(), players[1].GetRotation());
			}
#endif

			foreach (NetPlayer player in players) {
				player?.Update();
			}
		}

#if !PUBLIC_RELEASE
		/// <summary>
		/// Update network debug IMGUI.
		/// </summary>
		public void DrawDebugGUI() {

			GUI.color = Color.white;

			foreach (NetPlayer player in players) {
				if (player != null) {
					player.DrawDebugGUI();
				}
			}

			Rect debugPanel = new Rect(10, 50, 500, 20);
			GUI.Label(debugPanel, "Time since last heartbeat: " + timeSinceLastHeartbeat);
			debugPanel.y += 20.0f;
			GUI.Label(debugPanel, "Time to send next heartbeat: " + timeToSendHeartbeat);
			debugPanel.y += 20.0f;
			GUI.Label(debugPanel, "Ping: " + ping);
			debugPanel.y += 20.0f;
			GUI.Label(debugPanel, "My clock: " + GetNetworkClock());
			debugPanel.y += 20.0f;
			GUI.Label(debugPanel, "Remote clock: " + remoteClock);
			debugPanel.y += 20.0f;
			GUI.Label(debugPanel, "State: " + state);
			debugPanel.y += 20.0f;

			netWorld.UpdateIMGUI();
		}
#endif

		/// <summary>
		/// Process handshake message received from the given steam id.
		/// </summary>
		/// <param name="senderSteamId">The steam id of the sender.</param>
		/// <param name="msg">Hand shake message.</param>
		private void HandleHandshake(Steamworks.CSteamID senderSteamId, Messages.HandshakeMessage msg) {
			if (IsHost) {
				if (players[1] != null) {
					Logger.Log("Received handshake from player but player is already here.");
					LeaveLobby();
					return;
				}

				// Setup THE PLAYER

				timeSinceLastHeartbeat = 0.0f;
				players[1] = new NetPlayer(this, netWorld, senderSteamId);

				// Player can be spawned here safely. Host is already in game and all game objects are here.

				players[1].Spawn();

				SendHandshake();
			}
			else {
				if (players[1] == null) {
					Logger.Log("Received handshake from host but host is not here.");
					LeaveLobby();
					return;
				}

				Logger.Log("CONNECTION ESTABLISHED!");

				MPController.Instance.LoadLevel("GAME");

				// Host will be spawned when game will be loaded and OnGameWorldLoad callback will be called.
			}

			players[1].hasHandshake = true;
		}

		/// <summary>
		/// Sends handshake to the connected player.
		/// </summary>
		private void SendHandshake() {
			Messages.HandshakeMessage message = new Messages.HandshakeMessage();
			message.clock = GetNetworkClock();
			BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);
		}

		/// <summary>
		/// Callback called when game world gets loaded.
		/// </summary>
		public void OnGameWorldLoad() {
			// If we are not online setup an lobby for players to connect.

			if (!IsOnline) {
				SetupLobby();
				return;
			}

			if (IsPlayer) {
				netWorld.AskForFullWorldSync();
			}
		}

		/// <summary>
		/// Get local player object.
		/// </summary>
		/// <returns>Local player object.</returns>
		public NetLocalPlayer GetLocalPlayer() {
			return (NetLocalPlayer)players[0];
		}

		/// <summary>
		/// Get network player object by steam id.
		/// </summary>
		/// <param name="steamId">The steam id used to find player for.</param>
		/// <returns>Network player object or null if there is not player matching given steam id.</returns>
		public NetPlayer GetPlayer(Steamworks.CSteamID steamId) {
			foreach (NetPlayer player in players) {
				if (player?.SteamId == steamId) {
					return player;
				}
			}
			return null;
		}


		/// <summary>
		/// Called after whole network world is loaded.
		/// </summary>
		public void OnNetworkWorldLoaded() {
			state = State.Playing;
		}
	}
}
