using UnityEngine;

namespace MSCMP.Game.Objects {
	/// <summary>
	/// Handles passenger seats.
	/// 
	/// This method appears to be easier than modifying current passenger seats of vehicles.
	/// </summary>
	/// <param name="other"></param>
	public class PassengerSeat : MonoBehaviour {
		public string VehicleType = null;
		public GameObject DriversSeat = null;

		GameObject player = null;
		GameObject trigger = null;

		CapsuleCollider playerCollider = null;

		bool canSit = false;
		bool isSitting = false;
		bool showGUI = false;

		CharacterMotor motor = null;

		GameObject guiGameObject = null;
		PlayMakerFSM iconsFsm = null;
		PlayMakerFSM textFsm = null;

		public delegate void OnEnter();
		public delegate void OnLeave();
		public OnEnter onEnter = () => {
			
		};
		public OnLeave onLeave = () => {
		
		};

		Vector3 currentPosition;

		/// <summary>
		/// Initialise passenger seat
		/// </summary>
		void Start() {
			Logger.Debug($"Passenger seat added, vehicle: {VehicleType}");

			guiGameObject = GameObject.Find("GUI");

			PlayMakerFSM[] fsms = guiGameObject.GetComponentsInChildren<PlayMakerFSM>();
			foreach (PlayMakerFSM fsm in fsms) {
				if (fsm.FsmName == "Logic") {
					iconsFsm = fsm;
					continue;
				}
				else if (fsm.FsmName == "SetText" && fsm.gameObject.name == "Interaction") {
					textFsm = fsm;
					continue;
				}
				if (iconsFsm != null && textFsm != null) {
					break;
				}
			}

			// Set seat position and size based on vehicle
			trigger = this.gameObject;

			// Van
			if (VehicleType.StartsWith("HAYOSIKO")) {
				trigger.transform.localPosition = new Vector3(-DriversSeat.transform.localPosition.x + 0.1f, -DriversSeat.transform.localPosition.y + 0.35f,  -DriversSeat.transform.localPosition.z - 0.7f);
			}

			// Truck
			if (VehicleType.StartsWith("GIFU")) {
				trigger.transform.localPosition = new Vector3(-DriversSeat.transform.localPosition.x, -DriversSeat.transform.localPosition.y, -DriversSeat.transform.localPosition.z + 0.15f);
			}

			// Old car
			if (VehicleType.StartsWith("RCO_RUSCKO")) {
				trigger.transform.localPosition = new Vector3(-DriversSeat.transform.localPosition.x, -DriversSeat.transform.localPosition.y, -DriversSeat.transform.localPosition.z - 0.15f);
			}

			// The impossible to drive car
			if (VehicleType.StartsWith("FERNDALE")) {
				trigger.transform.localPosition = new Vector3(-DriversSeat.transform.localPosition.x + 0.1f, -DriversSeat.transform.localPosition.y, -DriversSeat.transform.localPosition.z - 0.6f);
			}

			// Satsuma
			if (VehicleType.StartsWith("SATSUMA")) {
				trigger.transform.localPosition = new Vector3(-DriversSeat.transform.localPosition.x, -DriversSeat.transform.localPosition.y, -DriversSeat.transform.localPosition.z + 0.15f);
			}

			// Destroys the cube mesh render
			GameObject.Destroy(trigger.GetComponentInChildren<MeshRenderer>());
		}

		/// <summary>
		/// Triggered on entering the passenger seat.
		/// </summary>
		/// <param name="other"></param>
		void OnTriggerEnter(Collider other) {
			if (other.gameObject.name == "PLAYER") {
				canSit = true;

				if (isSitting == false) {
					showGUI = true;
				}

				if (player == null) {
					player = other.gameObject;
					motor = player.GetComponentInChildren<CharacterMotor>();
				}

				if (playerCollider == null) {
					playerCollider = player.GetComponentInChildren<CapsuleCollider>();
					playerCollider.enabled = false;
				}
			}
		}

		/// <summary>
		/// Triggered on leaving the passenger seat.
		/// </summary>
		/// <param name="other"></param>
		void OnTriggerExit(Collider other) {
			if (other.gameObject.name == "PLAYER") {
				canSit = false;

				showGUI = false;
				iconsFsm.Fsm.GetFsmBool("GUIpassenger").Value = false;
				textFsm.Fsm.GetFsmString("GUIinteraction").Value = "";
			}
		}

		/// <summary>
		/// Called every frame.
		/// </summary>
		void Update() {
			if (showGUI == true) {
				// Yep, this needs to be called on update. Thanks MSC.
				textFsm.Fsm.GetFsmString("GUIinteraction").Value = "ENTER PASSENGER MODE";
				iconsFsm.Fsm.GetFsmBool("GUIpassenger").Value = true;
			}

			if (Input.GetKeyDown(KeyCode.Return) == true) {
				// Enter seat
				if (isSitting == false && canSit == true) {
					isSitting = true;

					player.transform.parent = this.gameObject.transform;
					currentPosition = player.transform.localPosition;
					motor.enabled = false;

					player.transform.FindChild("Sphere").GetComponentInChildren<SphereCollider>().enabled = false;

					showGUI = false;
					iconsFsm.Fsm.GetFsmBool("GUIpassenger").Value = false;
					textFsm.Fsm.GetFsmString("GUIinteraction").Value = "";

					Collider[] colliders = player.GetComponents<Collider>();
					foreach (Collider col in colliders) {
						col.enabled = false;
					}

					onEnter();
				}
				// Leave seat
				else if (isSitting == true) {
					isSitting = false;

					Collider[] colliders = player.GetComponents<Collider>();
					foreach (Collider col in colliders) {
						col.enabled = true;
					}

					player.transform.parent = null;

					// Resets player rotation on leaving the seat
					player.transform.rotation = new Quaternion(0, player.transform.rotation.y, 0, player.transform.rotation.w);

					motor.enabled = true;

					showGUI = true;

					onLeave();
				}
			}

			// Keep player in the correct position in the seat.
			if (isSitting == true) {
				player.transform.localPosition = currentPosition;
			}
		}
	}
}
