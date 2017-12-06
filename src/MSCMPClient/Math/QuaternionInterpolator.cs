using UnityEngine;

namespace MSCMP.Math {
	class QuaternionInterpolator {

		Quaternion current = new Quaternion();
		Quaternion source = new Quaternion();
		Quaternion target = new Quaternion();

		public Quaternion Current {
			get {
				return current;
			}
		}

		public void SetTarget(Quaternion quat) {
			source = current;
			target = quat;
		}

		public void Teleport(Quaternion quat) {
			current = source = target = quat;
		}

		public Quaternion Evaluate(float alpha) {
			current = Quaternion.Slerp(source, target, alpha);
			return current;
		}
	}
}
