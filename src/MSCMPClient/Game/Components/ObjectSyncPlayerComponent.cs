using UnityEngine;
using System.Threading.Tasks;

namespace MSCMP.Game.Components {
	/// <summary>
	/// Attached to player, uses radius around player to determine object sync frequency.
	/// </summary>
	class ObjectSyncPlayerComponent : MonoBehaviour {
		/// <summary>
		/// Ran on script start.
		/// </summary>
		void Start() {

		}

		/// <summary>
		/// Called on object entering trigger.
		/// </summary>
		/// <param name="other"></param>
		void OnTriggerEnter(Collider other) {
			ObjectSyncComponent syncComponent = other.GetComponent<ObjectSyncComponent>();
			if (syncComponent != null) {
				Task t = new Task(syncComponent.SendEnterSync);
				t.Start();
			}
		}

		/// <summary>
		/// Called on object exiting trigger.
		/// </summary>
		/// <param name="other"></param>
		void OnTriggerExit(Collider other) {
			ObjectSyncComponent syncComponent = other.GetComponent<ObjectSyncComponent>();
			if (syncComponent != null) {
				Task t = new Task(syncComponent.SendExitSync);
				t.Start();
			}
		}
	}
}
