using HutongGames.PlayMaker;
using UnityEngine;

namespace MSCMP.Game.Objects {
	class GameVehicle {

		GameObject gameObject = null;

		CarDynamics dynamics = null;


		public delegate void OnEnter();
		public delegate void OnLeave();
		public OnEnter onEnter = () => {
			Logger.Log("On Enter");
		};
		public OnLeave onLeave = () => {
			Logger.Log("On Leave");
		};

		public string Name
		{
			get {
				return gameObject != null ? gameObject.name : "";
			}
		}

		public Transform VehicleTransform {
			get {
				return gameObject.transform;
			}
		}

		bool hasDriver = false;


		/// <summary>
		/// PlayMaker state action executed when local player enters vehicle.
		/// </summary>
		private class OnEnterAction : FsmStateAction {
			private GameVehicle vehicle;

			public OnEnterAction(GameVehicle veh) {
				vehicle = veh;
			}

			public override void OnEnter() {
				Utils.CallSafe("OnLeaveHandler", () => {
					if (Fsm.PreviousActiveState.Name == "Death") {
						vehicle.hasDriver = true;
						if (vehicle.onEnter != null) {
							vehicle.onEnter();
						}
					}
				});
				Finish();
			}
		}

		/// <summary>
		/// PlayMaker state action executed when local player leaves vehicle.
		/// </summary>
		private class OnLeaveAction : FsmStateAction {
			private GameVehicle vehicle;

			public OnLeaveAction(GameVehicle veh) {
				vehicle = veh;
			}

			public override void OnEnter() {
				Utils.CallSafe("OnLeaveHandler", () => {
					if (Fsm.PreviousActiveState.Name == "Create player") {
						vehicle.hasDriver = false;
						if (vehicle.onLeave != null) {
							vehicle.onLeave();
						}
					}
				});
				Finish();
			}
		}

		public GameVehicle(GameObject go) {
			gameObject = go;

			dynamics = gameObject.GetComponent<CarDynamics>();

			/*PlayMakerFSM loadingFsm = Utils.GetPlaymakerScriptByName(gameObject, "LOD");
			if (loadingFsm != null) {
				Component.Destroy(loadingFsm);
			}*/


			PlayMakerFSM[] fsms = gameObject.GetComponentsInChildren<PlayMakerFSM>();

			Logger.Log("FSMS of " + gameObject.name);
			foreach (var fsm in fsms) {
				if (fsm.FsmName != "PlayerTrigger") {
					continue;
				}

				FsmState playerInCarState = fsm.Fsm.GetState("Player in car");
				FsmState waitForPlayerState = fsm.Fsm.GetState("Wait for player");

				if (waitForPlayerState != null) {
					PlayMakerUtils.AddNewAction(waitForPlayerState, new OnLeaveAction(this));
				}

				if (playerInCarState != null) {
					PlayMakerUtils.AddNewAction(playerInCarState, new OnEnterAction(this));
				}
			}
		}

		public void SetPosAndRot(Vector3 pos, Quaternion rot) {
			Transform transform = gameObject.transform;
			transform.position = pos;
			transform.rotation = rot;
		}

		public void UpdateIMGUI() {


			/*Vector3 spos = Camera.main.WorldToScreenPoint(gameObject.transform.position);
			spos.y = Screen.height - spos.y;

			string vinfo = gameObject.name + "\nHas driver: " + hasDriver + "\n";
			GUI.Label(new Rect(spos.x, spos.y, 500, 200), vinfo);*/

		}

	}
}
