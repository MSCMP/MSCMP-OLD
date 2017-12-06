using HutongGames.PlayMaker;
using System.Collections.Generic;

namespace MSCMP {
	/// <summary>
	/// Class containing PlayMaker utils.
	/// </summary>
	static class PlayMakerUtils {

		/// <summary>
		/// Add new global transition from the given event to the state name to the given PlayMaker FSM.
		/// </summary>
		/// <param name="fsm">The PlayMaker FSM to add global transition for.</param>
		/// <param name="ev">The event triggering the transition.</param>
		/// <param name="stateName">The state this transition activates.</param>
		static public void AddNewGlobalTransition(PlayMakerFSM fsm, FsmEvent ev, string stateName) {
			FsmTransition[] oldTransitions = fsm.FsmGlobalTransitions;
			List<FsmTransition> temp = new List<FsmTransition>();
			foreach (FsmTransition t in oldTransitions) {
				temp.Add(t);
			}
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
			foreach (var v in oldActions) {
				temp.Add(v);
			}
			state.Actions = temp.ToArray();
		}
	}
}
