using UnityEngine;

using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using MSCMP.Network;
using MSCMP.Game;

namespace MSCMP {
	/// <summary>
	/// Main multiplayer mode controller component.
	/// </summary>
	class MPController : MonoBehaviour {

		public const string MOD_VERSION_STRING = "0.1";

		public static MPController Instance = null;

#if !PUBLIC_RELEASE
		/// <summary>
		/// Various utilities used for development.
		/// </summary>
		DevTools devTools = new DevTools();
#endif

		/// <summary>
		/// The file used for logging. TODO: Proper logger.
		/// </summary>
		public static StreamWriter logFile = new StreamWriter(Client.GetPath("clientLog.txt"), false);

		/// <summary>
		/// Object managing whole networking.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// Name of the currently loaded level.
		/// </summary>
		string currentLevelName = "";

		/// <summary>
		/// Game object representing local player.
		/// </summary>
		GameObject localPlayer = null;

		/// <summary>
		/// Current scroll value of the invite panel.
		/// </summary>
		Vector2 friendsScrollViewPos = new Vector2();

		/// <summary>
		/// State override of the friend list.
		/// </summary>
		Dictionary<Steamworks.CSteamID, string> friendStateOverride = new Dictionary<Steamworks.CSteamID, string>();

		/// <summary>
		/// Game animations database.
		/// </summary>
		GameAnimDatabase gameAnimDatabase = new GameAnimDatabase();

		/// <summary>
		/// Asset bundle containing multiplayer mod content.
		/// </summary>
		AssetBundle assetBundle = null;

		/// <summary>
		/// The mod logo texture.
		/// </summary>
		Texture2D modLogo = null;

		/// <summary>
		/// Game world manager object.
		/// </summary>
		GameWorld gameWorld = new GameWorld();

		MPController() {
			Instance = this;
		}

		~MPController() {
			Instance = null;
		}

		void Start() {
			logFile.AutoFlush = true;

			string assetBundlePath = Client.GetPath("../../data/mpdata");
			if (!File.Exists(assetBundlePath)) {
				logFile.WriteLine("Cannot find mpdata asset bundle.");
				Process.GetCurrentProcess().Kill();
				return;
			}

			assetBundle = AssetBundle.CreateFromFile(assetBundlePath);

			Steamworks.SteamAPI.Init();

			DontDestroyOnLoad(this.gameObject);

			netManager = new NetManager(logFile);

			modLogo = LoadAsset<Texture2D>("Assets/Textures/MSCMPLogo.png");
		}

		/// <summary>
		/// Callback called when unity loads new event.
		/// </summary>
		/// <param name="newLevelName"></param>
		void OnLevelSwitch(string newLevelName) {
			if (newLevelName == "GAME") {
				gameAnimDatabase.Rebuild();
				gameWorld.OnLoad();

				netManager.OnGameWorldLoad();
			}
			else if (newLevelName == "MainMenu") {
				if (netManager.IsOnline) {
					netManager.Disconnect();
				}
			}
		}

		/// <summary>
		/// Updates IMGUI of the multiplayer.
		/// </summary>
		void OnGUI() {
			GUI.color = Color.white;
			GUI.Label(new Rect(2, Screen.height - 18, 500, 20), "MSCMP " + MOD_VERSION_STRING);

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

			// Friends widget.

			UpdateInvitePanel();

#if !PUBLIC_RELEASE
			devTools.OnGUI(localPlayer);

			netManager.DrawDebugGUI();
#endif
		}

		/// <summary>
		/// Updates invite panel IMGUI.
		/// </summary>
		private void UpdateInvitePanel() {

			if (!netManager.IsHost || netManager.IsNetworkPlayerConnected()) {
				friendStateOverride.Clear();
				return;
			}

			GUI.color = Color.white;

			Steamworks.EFriendFlags friendFlags = Steamworks.EFriendFlags.k_EFriendFlagImmediate;
			int friendsCount = Steamworks.SteamFriends.GetFriendCount(friendFlags);

			List<Steamworks.CSteamID> onlineFriends = new List<Steamworks.CSteamID>();
			for (int i = 0; i < friendsCount; ++i) {

				Steamworks.CSteamID friendSteamId = Steamworks.SteamFriends.GetFriendByIndex(i, friendFlags);

				if (Steamworks.SteamFriends.GetFriendPersonaState(friendSteamId) == Steamworks.EPersonaState.k_EPersonaStateOffline) {
					continue;
				}

				onlineFriends.Add(friendSteamId);
			}

			int onlineFriendsCount = onlineFriends.Count;
			friendsScrollViewPos = GUI.BeginScrollView(new Rect(0, 30.0f, 270.0f, 400.0f), friendsScrollViewPos, new Rect(0, 0, 260.0f, 20.0f * (1 + onlineFriendsCount)));

			for (int i = 0; i < onlineFriendsCount; ++i) {
				Steamworks.CSteamID friendSteamId = onlineFriends[i];

				string friendName = Steamworks.SteamFriends.GetFriendPersonaName(friendSteamId);

				Rect friendRect = new Rect(2, 1 + 20 * i, 200.0f, 20);

				// TODO: Draw nice state here - for now let's assume all players are ready to join.
				string state = "";
				if (friendStateOverride.ContainsKey(friendSteamId)) {
					state = " - " + friendStateOverride[friendSteamId];
				}

				GUI.Label(friendRect, friendName + state);

				friendRect.x += 200.0f;
				friendRect.width = 50.0f;

				if (GUI.Button(friendRect, "Invite")) {
					if (netManager.InviteToMyLobby(friendSteamId)) {
						friendStateOverride.Add(friendSteamId, "INVITED!");
					}
					else {
						friendStateOverride.Add(friendSteamId, "FAILED TO INVITE");
					}

				}
			}

			GUI.EndScrollView();
		}

		/// <summary>
		/// Update multiplayer state.
		/// </summary>
		void Update() {
			try {
				Steamworks.SteamAPI.RunCallbacks();

				netManager.Update();

				// Handle level changes.

				string loadedLevelName = Application.loadedLevelName;
				if (loadedLevelName != currentLevelName) {
					OnLevelSwitch(loadedLevelName);
					currentLevelName = loadedLevelName;
				}

				// Development stuff.
#if !PUBLIC_RELEASE
				devTools.Update();

				if (localPlayer == null) {
					localPlayer = GameObject.Find("PLAYER");
				}
				else {
					devTools.UpdatePlayer(localPlayer);
				}
#endif
			}
			catch (Exception e) {
				logFile.WriteLine("Exception during update: " + e.Message);
				logFile.WriteLine(e.StackTrace);
				Application.Quit();
			}
		}

		/// <summary>
		/// Loads asset from multiplayer mod asset bundle.
		/// </summary>
		/// <typeparam name="T">The type of the asset to load.</typeparam>
		/// <param name="name">The name of the asset to load.</param>
		/// <returns>Loaded asset.</returns>
		T LoadAsset<T>(string name) where T : UnityEngine.Object {
			return assetBundle.LoadAsset<T>(name);
		}

		/// <summary>
		/// Wrapper around unitys load level method to call OnLevelSwitch even if level is the same.
		/// </summary>
		/// <param name="levelName">The name of the level to load.</param>
		public void LoadLevel(string levelName) {
			if (currentLevelName == levelName) {
				OnLevelSwitch(levelName);
				return;
			}

			Application.LoadLevel(levelName);
		}
	}
}
