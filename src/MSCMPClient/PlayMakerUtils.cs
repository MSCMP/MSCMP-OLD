using HutongGames.PlayMaker;
using System.Collections.Generic;
using UnityEngine;

namespace MSCMP {
	/// <summary>
	/// Class containing PlayMaker utils.
	/// </summary>
	static class PlayMakerUtils {

		/// <summary>
		/// Add new global transition from the given event to the state name to the given
		/// PlayMaker FSM.
		/// </summary>
		/// <param name="fsm">The PlayMaker FSM to add global transition for.</param>
		/// <param name="ev">The event triggering the transition.</param>
		/// <param name="stateName">The state this transition activates.</param>
		static public void AddNewGlobalTransition(
				PlayMakerFSM fsm, FsmEvent ev, string stateName) {
			FsmTransition[] oldTransitions = fsm.FsmGlobalTransitions;
			List<FsmTransition> temp = new List<FsmTransition>();
			foreach (FsmTransition t in oldTransitions) { temp.Add(t); }
			FsmTransition transition = new FsmTransition();
			transition.FsmEvent = ev;
			transition.ToState = stateName;
			temp.Add(transition);

			fsm.Fsm.GlobalTransitions = temp.ToArray();
		}

		/// <summary>
		/// Add new action into play maker state.
		/// </summary>
		/// <param name="state">The state to add action to.</param>
		/// <param name="action">The action to add.</param>
		static public void AddNewAction(FsmState state, FsmStateAction action) {
			FsmStateAction[] oldActions = state.Actions;
			List<FsmStateAction> temp = new List<FsmStateAction>();
			temp.Add(action);
			foreach (var v in oldActions) { temp.Add(v); }
			state.Actions = temp.ToArray();
		}

		/// <summary>
		/// Removes an event and global transition from an fsm
		/// </summary>
		/// <param name="fsm">The FSM you want to delete it from</param>
		/// <param name="eventName">The event(and global transition) name</param>
		static public void RemoveEvent(PlayMakerFSM fsm, string eventName) {
			FsmTransition[] oldTransitions = fsm.FsmGlobalTransitions;
			List<FsmTransition> temp = new List<FsmTransition>();
			foreach (FsmTransition t in oldTransitions) {
				if (t.EventName != eventName) temp.Add(t);
			}
			fsm.Fsm.GlobalTransitions = temp.ToArray();

			FsmEvent[] oldEvents = fsm.Fsm.Events;
			List<FsmEvent> temp2 = new List<FsmEvent>();
			foreach (FsmEvent t in oldEvents) {
				if (t.Name != eventName) temp2.Add(t);
			}
			fsm.Fsm.Events = temp2.ToArray();
		}

		/// <summary>
		/// Set a gameObject's state
		/// </summary>
		/// <param name="gameObject">The gameObject you want to set</param>
		/// <param name="fsmName">The FSM that contains the state</param>
		/// <param name="state">The name of the state</param>
		static public void SetToState(
				GameObject gameObject, string fsmName, string state) {
			string hookedEventName = state + "-MSCMP";
			PlayMakerFSM fsm = Utils.GetPlaymakerScriptByName(gameObject, fsmName);

			FsmEvent ourEvent = fsm.Fsm.GetEvent(hookedEventName);
			PlayMakerUtils.AddNewGlobalTransition(fsm, ourEvent, state);

			fsm.SendEvent(hookedEventName);
			RemoveEvent(fsm, hookedEventName);
		}
	}
}
