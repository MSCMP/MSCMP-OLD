using HutongGames.PlayMaker;
using System.Collections.Generic;
using UnityEngine;

namespace MSCMP.Game.Objects {

	/// <summary>
	/// Game doors wrapper.
	/// </summary>
	class GameDoor {
		/// <summary>
		/// Game doors game obejct.
		/// </summary>
		GameObject go = null;


		/// <summary>
		/// Doors PlayMaker finite state machine.
		/// </summary>
		PlayMakerFSM fsm = null;

		/// <summary>
		/// Are doors open?
		/// </summary>
		public bool IsOpen
		{
			get {
				return fsm.FsmVariables.FindFsmBool("DoorOpen").Value;
			}
		}

		/// <summary>
		/// Position of the doors in world.
		/// </summary>
		public Vector3 Position {
			get {
				return go.transform.position;
			}
		}

		public delegate void OnOpen();
		public delegate void OnClose();

		/// <summary>
		/// Callback called when doors are opened.
		/// </summary>
		public OnOpen onOpen;

		/// <summary>
		/// Callback called when doors are closed.
		/// </summary>
		public OnClose onClose;

		private const string OPEN_EVENT_NAME = "OPEN";
		private const string CLOSE_EVENT_NAME = "CLOSE";

		private const string MP_OPEN_EVENT_NAME = "MPOPEN";
		private const string MP_CLOSE_EVENT_NAME = "MPCLOSE";

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameObject">Game object of the doors to represent by this wrapper.</param>
		public GameDoor(GameObject gameObject) {
			go = gameObject;
			fsm = Utils.GetPlaymakerScriptByName(go, "Use");
			if (fsm.Fsm.HasEvent(MP_OPEN_EVENT_NAME)) {
				Logger.Log("Failed to hook game door " + go.name + ". It is already hooked.");
				return;
			}

			FsmEvent mpOpenEvent = fsm.Fsm.GetEvent(MP_OPEN_EVENT_NAME);
			FsmEvent mpCloseEvent = fsm.Fsm.GetEvent(MP_CLOSE_EVENT_NAME);

			PlayMakerUtils.AddNewGlobalTransition(fsm, mpOpenEvent, "Open door");
			PlayMakerUtils.AddNewGlobalTransition(fsm, mpCloseEvent, "Close door");

			PlayMakerUtils.AddNewAction(fsm.Fsm.GetState("Open door"), new OnOpenDoorsAction(this));
			PlayMakerUtils.AddNewAction(fsm.Fsm.GetState("Close door"), new OnCloseDoorsAction(this));
		}

		/// <summary>
		/// PlayMaker state action executed when doors are opened.
		/// </summary>
		private class OnOpenDoorsAction : FsmStateAction {
			private GameDoor gameDoor;

			public OnOpenDoorsAction(GameDoor door) {
				gameDoor = door;
			}

			public override void OnEnter() {
				Finish();

				// If open was not triggered by local player do not send call the callback.

				if (State.Fsm.LastTransition.EventName != OPEN_EVENT_NAME) {
					return;
				}

				gameDoor.onOpen();
			}
		}

		/// <summary>
		/// PlayMaker state action executed when doors are closed.
		/// </summary>
		private class OnCloseDoorsAction : FsmStateAction {
			private GameDoor gameDoor;

			public OnCloseDoorsAction(GameDoor door) {
				gameDoor = door;
			}

			public override void OnEnter() {

				Finish();

				// If close was not triggered by local player do not send call the callback.

				if (State.Fsm.LastTransition.EventName != CLOSE_EVENT_NAME) {
					return;
				}

				gameDoor.onClose();

			}
		}

		/// <summary>
		/// Opens or closes the doors.
		/// </summary>
		/// <param name="open">Open or close?</param>
		public void Open(bool open) {
			if (open) {
				fsm.SendEvent(MP_OPEN_EVENT_NAME);
			}
			else {
				fsm.SendEvent(MP_CLOSE_EVENT_NAME);
			}
		}

		/// <summary>
		/// Calculate distance between doors and the given point.
		/// </summary>
		/// <param name="point">The point to calculate distance to.</param>
		/// <returns>Distance between points and the doors.</returns>
		public float DistanceToPoint(Vector3 point) {
			return (go.transform.position - point).magnitude;
		}
	}
}
