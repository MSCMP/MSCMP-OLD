using UnityEngine;

using System.Collections.Generic;
using System.IO;
using MSCMP.Network;
using System;

namespace MSCMP {
	class MPController : MonoBehaviour {


		DevTools devTools = new DevTools();

		public static StreamWriter logFile = new StreamWriter(Client.GetPath("clientLog.txt"), false);

		NetManager netManager = null;

		void Start() {
			logFile.AutoFlush = true;

			Steamworks.SteamAPI.Init();

			DontDestroyOnLoad(this.gameObject);

			netManager = new NetManager(logFile);

			// Application.LoadLevel("GAME");
		}

		void OnLevelSwitch(string newLevelName) {
			if (newLevelName == "GAME" && !netManager.IsOnline) {
				netManager.SetupLobby();
			}
			else if (newLevelName == "Main Menu") {
				if (netManager.IsOnline) {
					netManager.LeaveLobby();
				}
			}
		}
		string currentLevelName = "";

		GameObject localPlayer = null;


		Vector2 friendsScrollViewPos = new Vector2();
		Dictionary<Steamworks.CSteamID, string> friendStateOverride = new Dictionary<Steamworks.CSteamID, string>();

		void OnGUI() {
			GUI.color = Color.white;
			GUI.Label(new Rect(2, Screen.height - 18, 500, 20), "MSCMP 0.1");


			if (netManager.IsOnline) {
				GUI.color = Color.green;
				GUI.Label(new Rect(2, 2, 500, 20), "ONLINE " + (netManager.IsHost ? "HOST" : "PLAYER"));
			}
			else {
				GUI.color = Color.red;
				GUI.Label(new Rect(2, 2, 500, 20), "OFFLINE");
			}
			GUI.color = Color.white;

			// Friends widget.

			if (netManager.IsHost) {


				Steamworks.EFriendFlags friendFlags = Steamworks.EFriendFlags.k_EFriendFlagImmediate;
				int friendsCount = Steamworks.SteamFriends.GetFriendCount(friendFlags);

				List<Steamworks.CSteamID> onlineFriends = new List<Steamworks.CSteamID>();
				for (int i = 0; i < friendsCount; ++i) {

					Steamworks.CSteamID friendSteamId = Steamworks.SteamFriends.GetFriendByIndex(i, friendFlags);

					if (Steamworks.SteamFriends.GetFriendPersonaState(friendSteamId) == Steamworks.EPersonaState.k_EPersonaStateOffline) {
						// logFile.WriteLine(Steamworks.SteamFriends.GetFriendPersonaName(friendSteamId));
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


			devTools.OnGUI(localPlayer);
			netManager.DrawDebugGUI();
		}


		void Update() {
			try {
				Steamworks.SteamAPI.RunCallbacks();

				netManager.Update();

				string loadedLevelName = Application.loadedLevelName;
				if (loadedLevelName != currentLevelName) {
					OnLevelSwitch(loadedLevelName);
					currentLevelName = loadedLevelName;
				}
				devTools.Update();

				if (localPlayer == null) {
					localPlayer = GameObject.Find("PLAYER");
				}
				else {
					devTools.UpdatePlayer(localPlayer);
				}
			}
			catch (Exception e) {
				logFile.WriteLine("Exception during update: " + e.Message);
				logFile.WriteLine(e.StackTrace);
				Application.Quit();
			}
		}
	}
}
