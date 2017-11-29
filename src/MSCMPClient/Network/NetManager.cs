using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MSCMP.Network {
	class NetManager {
		private const int MAX_PLAYERS = 2;
		private Steamworks.Callback<Steamworks.GameLobbyJoinRequested_t> gameLobbyJoinRequestedCallback = null;
		private Steamworks.Callback<Steamworks.P2PSessionRequest_t> p2pSessionRequestCallback = null;
		private Steamworks.CallResult<Steamworks.LobbyCreated_t> lobbyCreatedCallResult = null;
		private Steamworks.CallResult<Steamworks.LobbyEnter_t> lobbyEnterCallResult = null;

		private StreamWriter logFile = null;

		public enum State {
			Idle,
			Host,
			Player
		}

		private State state;

		public bool IsHost {
			get { return state == State.Host; }
		}
		public bool IsPlayer {
			get { return state == State.Player; }
		}
		public bool IsOnline {
			get { return state != State.Idle; }
		}

		private Steamworks.CSteamID currentLobbyId = Steamworks.CSteamID.Nil;

		private NetPlayer[] players = new NetPlayer[MAX_PLAYERS];

		delegate void HandleMessageLowLevel(Steamworks.CSteamID sender, BinaryReader reader);
		private Dictionary<byte, HandleMessageLowLevel> messageHandlers = new Dictionary<byte, HandleMessageLowLevel>();

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

		public NetManager(StreamWriter logFile) {
			this.logFile = logFile;
			state = State.Idle;

			// Setup local player.
			players[0] = new NetLocalPlayer(this, Steamworks.SteamUser.GetSteamID());

			p2pSessionRequestCallback = Steamworks.Callback<Steamworks.P2PSessionRequest_t>.Create((Steamworks.P2PSessionRequest_t result) => {
				if (!Steamworks.SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote)) {
					logFile.WriteLine("Accepted p2p session with " + result.m_steamIDRemote.ToString());
				}
			});

			gameLobbyJoinRequestedCallback = Steamworks.Callback<Steamworks.GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

			lobbyCreatedCallResult = new Steamworks.CallResult<Steamworks.LobbyCreated_t>((Steamworks.LobbyCreated_t result, bool ioFailure) => {
				if (result.m_eResult != Steamworks.EResult.k_EResultOK) {
					logFile.WriteLine("Oh my fucking god i failed to create a lobby for you. Please forgive me. (result: " + result.m_eResult + ")");
					return;
				}

				logFile.WriteLine("Hey you! I have lobby id for you! " + result.m_ulSteamIDLobby);

				state = State.Host;
				currentLobbyId = new Steamworks.CSteamID(result.m_ulSteamIDLobby);
			});

			lobbyEnterCallResult = new Steamworks.CallResult<Steamworks.LobbyEnter_t>((Steamworks.LobbyEnter_t result, bool ioFailure) => {
				if (result.m_EChatRoomEnterResponse != (uint)Steamworks.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess) {
					logFile.WriteLine("Oh my fucking god i failed to join the lobby for you. Please forgive me. (reponse: " + result.m_EChatRoomEnterResponse + ")");

					players[1] = null;
					return;
				}

				logFile.WriteLine("Oh hello! " + result.m_ulSteamIDLobby);

				state = State.Player;
				currentLobbyId = new Steamworks.CSteamID(result.m_ulSteamIDLobby);

				Messages.HandshakeMessage message = new Messages.HandshakeMessage();
				message.clock = GetNetworkClock();
				BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);
			});

			BindMessageHandler((Steamworks.CSteamID sender, Messages.HandshakeMessage msg) => {
				remoteClock = msg.clock;
				HandleHandshake(sender);
			});

			BindMessageHandler((Steamworks.CSteamID sender, Messages.PlayerSyncMessage msg) => {
				if (players[1] == null) {
					logFile.WriteLine("Received synchronization packet but no remote player is currently connected.");
					return;
				}

				players[1].HandleSynchronize(msg);
			});

			BindMessageHandler((Steamworks.CSteamID sender, Messages.HeartbeatMessage msg) => {
				var message = new Messages.HeartbeatResponseMessage();
				message.clientClock = msg.clientClock;
				message.clock = GetNetworkClock();
				BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);
			});

			BindMessageHandler((Steamworks.CSteamID sender, Messages.HeartbeatResponseMessage msg) => {
				ping = (uint)(GetNetworkClock() - msg.clientClock);

				// TODO: Some smart lag compensation.
				remoteClock = msg.clock;

				timeSinceLastHeartbeat = 0.0f;
			});

			BindMessageHandler((Steamworks.CSteamID sender, Messages.DisconnectMessage msg) => {

				CleanupPlayer();

				// Go to main menu if we are normal player - the session just closed.

				logFile.WriteLine("OH NO MY FRIEND JUST LOST HIS LIFE ;-;");
				if (IsPlayer) {
					logFile.WriteLine("AND I NEED TO GO TO MAIN MENU ;-;");
					LeaveLobby();

					Application.LoadLevel("MainMenu");
				}
			});
		}

		private ulong GetNetworkClock() {
			long epochTicks = new DateTime(1970, 1, 1).Ticks;
			long unixTime = ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
			return (ulong)unixTime;
		}

		delegate void MessageHandler<T>(Steamworks.CSteamID Sender, T Message);

		private void BindMessageHandler<T>(MessageHandler<T> Handler) where T: INetMessage, new() {
			T message = new T();

			messageHandlers.Add(message.MessageId, (Steamworks.CSteamID sender, BinaryReader reader) => {
				if (! message.Read(reader)) {
					logFile.WriteLine("Failed to read network message " + message.MessageId + " received from " + sender.ToString());
					return;
				}
				Handler(sender, message);
			});
		}

		public bool BroadcastMessage<T>(T message, Steamworks.EP2PSend sendType, int channel = 0) where T : INetMessage {
			if (players[1] == null) {
				return false;
			}
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);

			writer.Write((byte)message.MessageId);
			if (! message.Write(writer)) {
				logFile.WriteLine("Failed to write network message " + message.MessageId);
				return false;
			}

			players[1].SendPacket(stream.GetBuffer(), sendType, channel);
			return true;
		}

		private void OnGameLobbyJoinRequested(Steamworks.GameLobbyJoinRequested_t request) {
			Steamworks.SteamAPICall_t apiCall = Steamworks.SteamMatchmaking.JoinLobby(request.m_steamIDLobby);
			if (apiCall == Steamworks.SteamAPICall_t.Invalid) {
				logFile.WriteLine("Unable to join lobby.");
				return;
			}

			logFile.WriteLine("Setup player.");
			// Setup remote player. The HOST.
			players[1] = new NetPlayer(this, request.m_steamIDFriend);

			lobbyEnterCallResult.Set(apiCall);
		}

		public bool SetupLobby() {
			logFile.WriteLine("Setting up lobby.");
			Steamworks.SteamAPICall_t apiCall = Steamworks.SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly, MAX_PLAYERS);
			if (apiCall == Steamworks.SteamAPICall_t.Invalid) {
				logFile.WriteLine("Unable to create lobby.");
				return false;
			}
			logFile.WriteLine("Waiting for lobby create reply..");
			lobbyCreatedCallResult.Set(apiCall);
			return true;
		}

		private void LeaveLobby() {
			Steamworks.SteamMatchmaking.LeaveLobby(currentLobbyId);
			currentLobbyId = Steamworks.CSteamID.Nil;
			state = State.Idle;
			logFile.WriteLine("Left lobby.");
		}

		public bool InviteToMyLobby(Steamworks.CSteamID invitee) {
			if (!IsHost) {
				return false;
			}
			return Steamworks.SteamMatchmaking.InviteUserToLobby(currentLobbyId, invitee);
		}

		public bool IsNetworkPlayerConnected() {
			return players[1] != null;
		}

		public void CleanupPlayer() {
			Steamworks.SteamNetworking.CloseP2PSessionWithUser(players[1].SteamId);
			players[1].Dispose();
			players[1] = null;
		}

		public void Disconnect() {
			BroadcastMessage(new Messages.DisconnectMessage(), Steamworks.EP2PSend.k_EP2PSendReliable);
			LeaveLobby();
		}

		private void UpdateHeartbeat() {
			if (!IsNetworkPlayerConnected()) {
				return;
			}

			timeSinceLastHeartbeat += Time.deltaTime;

			if (timeSinceLastHeartbeat >= TIMEOUT_TIME) {
				CleanupPlayer();
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

		private void ProcessMessages() {
			uint size = 0;
			while (Steamworks.SteamNetworking.IsP2PPacketAvailable(out size)) {
				if (size == 0) {
					logFile.WriteLine("Received empty p2p packet");
					continue;
				}

				byte[] data = new byte[size];

				uint msgSize = 0;
				Steamworks.CSteamID senderSteamId = Steamworks.CSteamID.Nil;
				if (!Steamworks.SteamNetworking.ReadP2PPacket(data, size, out msgSize, out senderSteamId)) {
					continue;
				}

				// TODO: Joining?
				if (msgSize != size || msgSize == 0) {
					logFile.WriteLine("Invalid packet size");
					continue;
				}

				MemoryStream stream = new MemoryStream(data);
				BinaryReader reader = new BinaryReader(stream);

				byte messageId = reader.ReadByte();
				if (messageHandlers.ContainsKey(messageId)) {
					messageHandlers[messageId](senderSteamId, reader);
				}
			}
		}

		public void Update() {
			if (!IsOnline) {
				return;
			}

			UpdateHeartbeat();
			ProcessMessages();

			foreach (NetPlayer player in players) {
				if (player != null) {
					player.Update();
				}
			}
		}

		public void DrawDebugGUI() {
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
		}

		private void HandleHandshake(Steamworks.CSteamID senderSteamId) {
			if (IsHost) {
				if (players[1] != null) {
					logFile.WriteLine("Received handshake from player but player is already here.");
					LeaveLobby();
					return;
				}

				// Setup THE PLAYER.

				players[1] = new NetPlayer(this, senderSteamId);

				Messages.HandshakeMessage message = new Messages.HandshakeMessage();
				message.clock = GetNetworkClock();
				BroadcastMessage(message, Steamworks.EP2PSend.k_EP2PSendReliable);
			}
			else {
				if (players[1] == null) {
					logFile.WriteLine("Received handshake from host but host is not here.");
					LeaveLobby();
					return;
				}

				logFile.WriteLine("CONNECTION ESTABLISHED!");

				Application.LoadLevel("GAME");
			}


			players[1].hasHandshake = true;
			players[1].Spawn();
		}
	}
}
