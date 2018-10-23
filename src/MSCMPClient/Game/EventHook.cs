using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace MSCMP.Game {
	/// <summary>
	/// Contains methods for hooking PlayMakerFSMs and syncing them with other clients.
	/// </summary>
	class EventHook {
		public Dictionary<int, PlayMakerFSM> fsms = new Dictionary<int, PlayMakerFSM>();
		public Dictionary<int, string> fsmEvents = new Dictionary<int, string>();

		public static EventHook Instance = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		public EventHook() {
			Instance = this;
		}

		/// <summary>
		/// Adds a PlayMaker Event hook.
		/// </summary>
		/// <param name="fsm">The PlayMakerFSM that contains the event to hook.</param>
		/// <param name="eventName">The name of the event to hook.</param>
		/// <param name="action">The action to perform on event firing.</param>
		/// <param name="actionOnExit">Should action be put ran on exitting instead of entering event?</param>
		public static void Add(PlayMakerFSM fsm, string eventName, Func<bool> action, bool actionOnExit = false) {
			if (fsm == null) {
				Client.Assert(true, "EventHook Add: Failed to hook event. (FSM is null)");
			}
			else {
				FsmState state = fsm.Fsm.GetState(eventName);
				if (state != null) {
					PlayMakerUtils.AddNewAction(state, new CustomAction(action, eventName, actionOnExit));
					FsmEvent mpEvent = fsm.Fsm.GetEvent("MP_" + eventName);
					PlayMakerUtils.AddNewGlobalTransition(fsm, mpEvent, eventName);
				}
			}
		}

		/// <summary>
		/// Adds a PlayMaker Event hook and syncs event with remote clients.
		/// </summary>
		/// <param name="fsm">The PlayMakerFSM that contains the event to hook.</param>
		/// <param name="eventName">The name of the event to hook.</param>
		/// <param name="action">Optional action to run. (Action runs before duplicate check!)</param>
		public static void AddWithSync(PlayMakerFSM fsm, string eventName, Func<bool> action = null) {
			if (fsm == null) {
				Client.Assert(true, "EventHook AddWithSync: Failed to hook event. (FSM is null)");
			}
			else {
				FsmState state = fsm.Fsm.GetState(eventName);
				if (state != null) {
					bool duplicate = false;
					int fsmId = Instance.fsmEvents.Count + 1;
					if (Instance.fsms.ContainsValue(fsm)) {
						duplicate = true;
						foreach (KeyValuePair<int, PlayMakerFSM> entry in Instance.fsms) {
							if (entry.Value == fsm) {
								fsmId = entry.Key;
								break;
							}
						}
					}
					int eventId = Instance.fsmEvents.Count + 1;
					Instance.fsms.Add(Instance.fsms.Count + 1, fsm);
					Instance.fsmEvents.Add(Instance.fsmEvents.Count + 1, eventName);

					PlayMakerUtils.AddNewAction(state, new CustomActionSync(Instance.fsms.Count, Instance.fsmEvents.Count, action));
					FsmEvent mpEvent = fsm.Fsm.GetEvent("MP_" + eventName);
					PlayMakerUtils.AddNewGlobalTransition(fsm, mpEvent, eventName);

					// Sync with host
					if (!Network.NetManager.Instance.IsHost && Network.NetManager.Instance.IsOnline && duplicate == false) {
						Network.NetLocalPlayer.Instance.RequestEventHookSync(fsmId);
					}
				}
			}
		}

		/// <summary>
		/// Runs the specified event.
		/// </summary>
		/// <param name="fsmID">FSM ID.</param>
		/// <param name="fsmEventID">FSM Event ID.</param>
		public static void HandleEventSync(int fsmID, int fsmEventID, string fsmEventName = "none") {
			try {
				if (fsmEventID == -1) {
					Instance.fsms[fsmID].SendEvent("MP_" + fsmEventName);
				}
				else {
					Instance.fsms[fsmID].SendEvent("MP_" + Instance.fsmEvents[fsmEventID]);
				}
			}
			catch {
				Client.Assert(true, $"Handle event sync failed! FSM not found at ID: {fsmID} - Ensure both players are using a new save created on the same version of My Summer Car. Any installed mods could also cause this error.");
			}
		}

		/// <summary>
		/// Responds to requests to sync an FSM event on the remote client.
		/// </summary>
		/// <param name="fsmID">FSM ID.</param>
		public static void SendSync(int fsmID) {
			PlayMakerFSM fsm = null;
			try {
				fsm = Instance.fsms[fsmID];
				string currentState = fsm.Fsm.ActiveStateName;
				if (currentState != "" || currentState != null) {
					Network.NetLocalPlayer.Instance.SendEventHookSync(fsmID, -1, currentState);
				}
			}
			catch {
				Logger.Debug($"Sync was request for an event, but the FSM wasn't found on this client!");
			}
		}

		/// <summary>
		/// Sync all events within a given FSM.
		/// </summary>
		/// <param name="fsm">FSM to sync Events of.</param>
		/// <param name="action">Optional action, default will only run events for the sync owner, or host is no one owns the object.</param>
		public static void SyncAllEvents(PlayMakerFSM fsm, Func<bool> action = null) {
			if (fsm == null) {
				Client.Assert(true, "EventHook SyncAllEvents: Failed to hook event. (FSM is null)");
				return;
			}
			FsmState[] states = fsm.FsmStates;

			int i = 0;
			while (i < states.Length) {
				EventHook.AddWithSync(fsm, states[i].Name, new Func<bool>(() => {
					if (action != null) {
						return action();
					}
					else {
						return false;
					}
				}));
				i++;
			}
		}

		/// <summary>
		/// Action used when adding event hooks.
		/// </summary>
		public class CustomAction : FsmStateAction {
			private Func<bool> action;
			private string eventName;
			private bool actionOnExit;

			public CustomAction(Func<bool> a, string eName, bool onExit) {
				action = a;
				eventName = eName;
				actionOnExit = onExit;
			}

			public bool RunAction(Func<bool> action) {
				return action();
			}

			public override void OnEnter() {
				if (actionOnExit == false) {
					if (RunAction(action) == true) {
						return;
					}
				}

				Finish();
			}

			public override void OnExit() {
				if (actionOnExit == true) {
					if (RunAction(action) == true) {
						return;
					}
				}

				Finish();
			}
		}

		/// <summary>
		/// Action used when adding sycned event hooks.
		/// </summary>
		public class CustomActionSync : FsmStateAction {
			private int fsmID;
			private int fsmEventID;
			private Func<bool> action;

			public CustomActionSync(int id, int eventID, Func<bool> a) {
				fsmID = id;
				fsmEventID = eventID;
				action = a;
			}
			
			public bool RunAction(Func<bool> action) {
				return action();
			}

			public override void OnEnter() {
				if (action != null) {
					if (RunAction(action) == true) {
						return;
					}
				}

				if (Instance.fsms[fsmID].Fsm.LastTransition.EventName == "MP_" + Instance.fsmEvents[fsmEventID]) {
					return;
				}

				Network.NetLocalPlayer.Instance.SendEventHookSync(fsmID, fsmEventID);

				Finish();
			}
		}
	}
}
