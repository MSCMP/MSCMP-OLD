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
		GameObject gameObject = null;

		/// <summary>
		/// Returns GameObject of the door.
		/// </summary>
		/// <returns>GameObject</returns>
		public GameObject GameObject {
			get { return gameObject; }
		}

		/// <summary>
		/// The owning object manager.
		/// </summary>
		GameDoorsManager manager = null;

		/// <summary>
		/// Doors PlayMaker finite state machine.
		/// </summary>
		PlayMakerFSM fsm = null;

		/// <summary>
		/// Are doors open?
		/// </summary>
		public bool IsOpen {
			get { return fsm.Fsm.HasEvent("MPOPEN"); }
		}

		/// <summary>
		/// Position of the doors in world.
		/// </summary>
		public Vector3 Position {
			get { return gameObject.transform.position; }
		}

		private const string OPEN_EVENT_NAME = "OPEN";
		private const string CLOSE_EVENT_NAME = "CLOSE";

		private const string MP_OPEN_EVENT_NAME = "MPOPEN";
		private const string MP_CLOSE_EVENT_NAME = "MPCLOSE";

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="manager">The manager that owns this door.</param>
		/// <param name="gameObject">Game object of the doors to represent by this
		/// wrapper.</param>
		public GameDoor(GameDoorsManager manager, GameObject gameObject) {
			this.manager = manager;
			this.gameObject = gameObject;

			fsm = Utils.GetPlaymakerScriptByName(gameObject, "Use");
			if (fsm.Fsm.HasEvent(MP_OPEN_EVENT_NAME)) {
				Logger.Log("Failed to hook game door " + gameObject.name +
						". It is already hooked.");
				return;
			}

			FsmEvent mpOpenEvent = fsm.Fsm.GetEvent(MP_OPEN_EVENT_NAME);
			FsmEvent mpCloseEvent = fsm.Fsm.GetEvent(MP_CLOSE_EVENT_NAME);

			PlayMakerUtils.AddNewGlobalTransition(fsm, mpOpenEvent, "Open door");
			PlayMakerUtils.AddNewGlobalTransition(fsm, mpCloseEvent, "Close door");

			PlayMakerUtils.AddNewAction(
					fsm.Fsm.GetState("Open door"), new OnOpenDoorsAction(this));
			PlayMakerUtils.AddNewAction(
					fsm.Fsm.GetState("Close door"), new OnCloseDoorsAction(this));
		}

		/// <summary>
		/// PlayMaker state action executed when doors are opened.
		/// </summary>
		private class OnOpenDoorsAction : FsmStateAction {
			private GameDoor gameDoor;

			public OnOpenDoorsAction(GameDoor door) { gameDoor = door; }

			public override void OnEnter() {
				Finish();

				// If open was not triggered by local player do not send call the callback.

				if (State.Fsm.LastTransition.EventName != OPEN_EVENT_NAME) { return; }

				// Notify manager about the action.

				gameDoor.manager.HandleDoorsAction(gameDoor, true);
			}
		}

		/// <summary>
		/// PlayMaker state action executed when doors are closed.
		/// </summary>
		private class OnCloseDoorsAction : FsmStateAction {
			private GameDoor gameDoor;

			public OnCloseDoorsAction(GameDoor door) { gameDoor = door; }

			public override void OnEnter() {
				Finish();

				// If close was not triggered by local player do not send call the callback.

				if (State.Fsm.LastTransition.EventName != CLOSE_EVENT_NAME) { return; }

				// Notify manager about the action.

				gameDoor.manager.HandleDoorsAction(gameDoor, false);
			}
		}

		/// <summary>
		/// Opens or closes the doors.
		/// </summary>
		/// <param name="open">Open or close?</param>
		public void Open(bool open) {
			if (open) {
				fsm.SendEvent(MP_OPEN_EVENT_NAME);
			} else {
				fsm.SendEvent(MP_CLOSE_EVENT_NAME);
			}
		}
	}
}
