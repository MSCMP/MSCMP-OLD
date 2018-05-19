using UnityEngine;

using System.Collections.Generic;
using System;
using MSCMP.Network;
using MSCMP.Game;
using MSCMP.Utilities;

namespace MSCMP {
	/// <summary>
	/// Main multiplayer mode controller component.
	/// </summary>
	class MPController : MonoBehaviour {

		public static MPController Instance = null;

		/// <summary>
		/// Object managing whole networking.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// Name of the currently loaded level.
		/// </summary>
		string currentLevelName = "";

		/// <summary>
		/// Current scroll value of the invite panel.
		/// </summary>
		Vector2 friendsScrollViewPos = new Vector2();

		/// <summary>
		/// The mod logo texture.
		/// </summary>
		Texture2D modLogo = null;

		/// <summary>
		/// Game world manager object.
		/// </summary>
		GameWorld gameWorld = new GameWorld();

		/// <summary>
		/// Console object.
		/// </summary>
		UI.Console console = new UI.Console();

		MPController() {
			Instance = this;
		}

		~MPController() {
			Instance = null;
		}

		void Start() {
			Steamworks.SteamAPI.Init();

			DontDestroyOnLoad(this.gameObject);

			netManager = new NetManager();

			modLogo = Client.LoadAsset<Texture2D>("Assets/Textures/MSCMPLogo.png");

			IMGUIUtils.Setup();

#if !PUBLIC_RELEASE
			// Skip splash screen in development builds.
			Application.LoadLevel("MainMenu");
#endif
		}

		/// <summary>
		/// Callback called when unity loads new event.
		/// </summary>
		/// <param name="newLevelName"></param>
		void OnLevelSwitch(string newLevelName) {
			if (currentLevelName == "GAME") {
				gameWorld.OnUnload();
			}

			if (newLevelName == "GAME") {
				gameWorld.OnLoad();
				netManager.OnGameWorldLoad();
				return;
			}

			// When leaving game to main menu disconenct from the session.

			if (currentLevelName == "GAME" && newLevelName == "MainMenu") {
				if (netManager.IsOnline) {
					netManager.Disconnect();
				}
			}
		}

		/// <summary>
		/// Updates IMGUI of the multiplayer.
		/// </summary>
		void OnGUI() {
			if (netManager.IsOnline) {
				netManager.DrawNameTags();
			}

			GUI.color = Color.white;
			GUI.Label(new Rect(2, Screen.height - 18, 500, 20), "MSCMP " + Client.GetMODDisplayVersion());

			GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);
			GUI.DrawTexture(new Rect(2, Screen.height - 80, 76, 66), modLogo);

			// Draw online state.

			if (netManager.IsOnline) {
				GUI.color = Color.green;
				GUI.Label(new Rect(2, 2, 500, 20), "ONLINE " + (netManager.IsHost ? "HOST" : "PLAYER"));
			}
			else {
				GUI.color = Color.red;
				GUI.Label(new Rect(2, 2, 500, 20), "OFFLINE");
			}

			MessagesList.Draw();

			// Friends widget.

			if (ShouldSeeInvitePanel()) {
				UpdateInvitePanel();
			}

#if !PUBLIC_RELEASE
			DevTools.OnGUI();

			if (DevTools.netStats) {
				netManager.DrawDebugGUI();
			}

			gameWorld.UpdateIMGUI();
#endif

			console.Draw();
		}

		/// <summary>
		/// The interval between each friend list updates from steam in seconds.
		/// </summary>
		const float FRIENDLIST_UPDATE_INTERVAL = 10.0f;

		/// <summary>
		/// Time left to next friend update.
		/// </summary>
		float timeToUpdateFriendList = 0.0f;

		struct FriendEntry {
			public Steamworks.CSteamID steamId;
			public string name;
			public bool playingMSC;
		}

		/// <summary>
		/// Time in seconds player can have between invite.
		/// </summary>
		const float INVITE_COOLDOWN = 10.0f;

		/// <summary>
		/// Current invite cooldown value.
		/// </summary>
		float inviteCooldown = 0.0f;

		List<FriendEntry> onlineFriends = new List<FriendEntry>();

		/// <summary>
		/// Steam id of the recently invited friend.
		/// </summary>
		Steamworks.CSteamID invitedFriendSteamId = new Steamworks.CSteamID();

		/// <summary>
		/// Check if invite panel is visible.
		/// </summary>
		/// <returns>true if invite panel is visible false otherwise</returns>
		bool IsInvitePanelVisible() {
			return PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerInMenu").Value;
		}

		/// <summary>
		/// Update friend list.
		/// </summary>
		void UpdateFriendList() {
			if (inviteCooldown > 0.0f) {
				inviteCooldown -= Time.deltaTime;
			}
			else {
				// Reset invited friend steam id.

				invitedFriendSteamId.Clear();
			}

			timeToUpdateFriendList -= Time.deltaTime;
			if (timeToUpdateFriendList > 0.0f) {
				return;
			}

			onlineFriends.Clear();

			Steamworks.EFriendFlags friendFlags = Steamworks.EFriendFlags.k_EFriendFlagImmediate;
			int friendsCount = Steamworks.SteamFriends.GetFriendCount(friendFlags);

			for (int i = 0; i < friendsCount; ++i) {
				Steamworks.CSteamID friendSteamId = Steamworks.SteamFriends.GetFriendByIndex(i, friendFlags);

				if (Steamworks.SteamFriends.GetFriendPersonaState(friendSteamId) == Steamworks.EPersonaState.k_EPersonaStateOffline) {
					continue;
				}



				FriendEntry friend = new FriendEntry();
				friend.steamId = friendSteamId;
				friend.name = Steamworks.SteamFriends.GetFriendPersonaName(friendSteamId);

				Steamworks.FriendGameInfo_t gameInfo;
				Steamworks.SteamFriends.GetFriendGamePlayed(friendSteamId, out gameInfo);
				friend.playingMSC = (gameInfo.m_gameID.AppID() == Client.GAME_APP_ID);

				if (friend.playingMSC) {
					onlineFriends.Insert(0, friend);
				}
				else {
					onlineFriends.Add(friend);
				}
			}

			timeToUpdateFriendList = FRIENDLIST_UPDATE_INTERVAL;
		}

		/// <summary>
		/// Should player see invite panel?
		/// </summary>
		/// <returns>true if invite panel should be visible, false otherwise</returns>
		bool ShouldSeeInvitePanel() {
			return netManager.IsHost && !netManager.IsNetworkPlayerConnected();
		}

		/// <summary>
		/// Updates invite panel IMGUI.
		/// </summary>
		private void UpdateInvitePanel() {
			if (!IsInvitePanelVisible()) {
				GUI.color = Color.white;
				GUI.Label(new Rect(0, Screen.height - 100, 200.0f, 20.0f), "[ESCAPE] - Invite friend");
				return;
			}

			const float invitePanelHeight = 400.0f;
			const float invitePanelWidth = 300.0f;
			const float rowHeight = 20.0f;
			Rect invitePanelRect = new Rect(10, Screen.height / 2 - invitePanelHeight / 2, invitePanelWidth, 20.0f);

			// Draw header

			GUI.color = new Color(1.0f, 0.5f, 0.0f, 0.8f);
			IMGUIUtils.DrawPlainColorRect(invitePanelRect);

			GUI.color = Color.white;
			invitePanelRect.x += 2.0f;
			GUI.Label(invitePanelRect, "Invite friend");
			invitePanelRect.x -= 2.0f;

			// Draw contents

			invitePanelRect.y += 21.0f;
			invitePanelRect.height = invitePanelHeight;

			GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.8f);
			IMGUIUtils.DrawPlainColorRect(invitePanelRect);

			GUI.color = new Color(1.0f, 0.5f, 0.0f, 0.8f);
			int onlineFriendsCount = onlineFriends.Count;
			invitePanelRect.height -= 2.0f;
			friendsScrollViewPos = GUI.BeginScrollView(invitePanelRect, friendsScrollViewPos, new Rect(0, 0, invitePanelWidth - 20.0f, 20.0f * onlineFriendsCount));

			int firstVisibleFriendId = (int)(friendsScrollViewPos.y / rowHeight);
			int maxVisibleFriends = (int)(invitePanelHeight / rowHeight);
			int lastIndex = firstVisibleFriendId + maxVisibleFriends + 1;
			if (lastIndex > onlineFriendsCount) {
				lastIndex = onlineFriendsCount;
			}
			for (int i = firstVisibleFriendId; i < lastIndex; ++i) {
				FriendEntry friend = onlineFriends[i];
				if (friend.playingMSC) {
					GUI.color = Color.green;
				}
				else {
					GUI.color = Color.white;
				}

				Rect friendRect = new Rect(2, 1 + rowHeight * i, 200.0f, rowHeight);

				GUI.Label(friendRect, friend.name);

				friendRect.x += 180.0f;
				friendRect.width = 100.0f;

				Steamworks.CSteamID friendSteamId = friend.steamId;

				if (invitedFriendSteamId == friendSteamId) {
					GUI.Label(friendRect, String.Format("INVITED! ({0:F1}s)", inviteCooldown));
					continue;
				}

				if (inviteCooldown > 0.0f) {
					continue;
				}

				if (GUI.Button(friendRect, "Invite")) {
					if (netManager.InviteToMyLobby(friendSteamId)) {
						invitedFriendSteamId = friendSteamId;
						inviteCooldown = INVITE_COOLDOWN;
					}
					else {
						UI.MPGUI.Instance.ShowMessageBox("Failed to invite friend due to steam error.");
					}

				}
			}

			GUI.EndScrollView();
		}

		/// <summary>
		/// Fixed update multiplayer state.
		/// </summary>
		void FixedUpdate() {
			Utils.CallSafe("FixedUpdate", () => {
				netManager.FixedUpdate();
			});
		}

		void OnLevelWasLoaded(int level) {
			string loadedLevelName = Application.loadedLevelName;
			OnLevelSwitch(loadedLevelName);
			currentLevelName = loadedLevelName;
		}

		/// <summary>
		/// Update multiplayer state.
		/// </summary>
		void LateUpdate() {
			Utils.CallSafe("Update", () => {
				Steamworks.SteamAPI.RunCallbacks();

				if (IsInvitePanelVisible()) {
					UpdateFriendList();
				}

				gameWorld.Update();
				netManager.Update();


				// Development stuff.
#if !PUBLIC_RELEASE
				DevTools.Update();
#endif
			});
		}

		/// <summary>
		/// Wrapper around unitys load level method to call OnLevelSwitch even if level is the same.
		/// </summary>
		/// <param name="levelName">The name of the level to load.</param>
		public void LoadLevel(string levelName) {
			Application.LoadLevel(levelName);
		}

		/// <summary>
		/// Can this client instance use save?
		/// </summary>
		public bool CanUseSave {
			get { return !netManager.IsOnline || netManager.IsHost; }
		}
	}
}
