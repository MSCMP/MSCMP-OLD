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

		public enum PacketId {
			Handshake,

			Synchronize
		}


		private Steamworks.CSteamID currentLobbyId = Steamworks.CSteamID.Nil;

		private NetPlayer[] players = new NetPlayer[MAX_PLAYERS];

		public NetManager(StreamWriter logFile) {
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

				SendPacket(SetupPacket(PacketId.Handshake), Steamworks.EP2PSend.k_EP2PSendReliable);
			});

			logFile.WriteLine("Network setup!");
		}

		public byte[] SetupPacket(PacketId packetId, int size = 0) {
			byte[] data = new byte[size + 1];
			data[0] = (byte)packetId;
			return data;
		}

		public void SendPacket(byte[] data, Steamworks.EP2PSend sendType, int channel = 0) {
			players[1].SendPacket(data, sendType, channel);
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

		public void LeaveLobby() {
			Steamworks.SteamMatchmaking.LeaveLobby(currentLobbyId);
			currentLobbyId = Steamworks.CSteamID.Nil;
			state = State.Idle;
			logFile.WriteLine("Leaved lobby.");
		}

		public bool InviteToMyLobby(Steamworks.CSteamID invitee) {
			if (!IsHost) {
				return false;
			}
			return Steamworks.SteamMatchmaking.InviteUserToLobby(currentLobbyId, invitee);
		}

		public void Update() {
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

				PacketId packetID = (PacketId)data[0];

				switch (packetID) {
					case PacketId.Handshake:
						HandleHandshake(senderSteamId);
						break;

					case PacketId.Synchronize:
						if (players[1].SteamId == senderSteamId) {
							MemoryStream stream = new MemoryStream(data);
							BinaryReader reader = new BinaryReader(stream);
							players[1].HandleSynchronize(reader);
						}
						break;
				}
			}

			foreach (NetPlayer player in players) {
				player.Update();
			}
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
				players[1].hasHandshake = true;
				players[1].Spawn();

				SendPacket(SetupPacket(PacketId.Handshake), Steamworks.EP2PSend.k_EP2PSendReliable);
			}
			else {
				if (players[1] == null) {
					logFile.WriteLine("Received handshake from host but host is not here.");
					LeaveLobby();
					return;
				}

				players[1].hasHandshake = true;
				logFile.WriteLine("CONNECTION ESTABLISHED!");
			}
		}
	}
}
