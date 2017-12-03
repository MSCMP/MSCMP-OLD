using UnityEngine;

namespace MSCMP.Math {
	class Vector3Interpolator {

		Vector3 current = new Vector3();
		Vector3 source = new Vector3();
		Vector3 target = new Vector3();

		public void SetTarget(Vector3 vec) {
			source = current;
			target = vec;
		}

		public void Teleport(Vector3 vec) {
			current = source = target = vec;
		}

		public Vector3 Evaluate(float alpha) {
			current = Vector3.Lerp(source, target, alpha);
			return current;
		}
	}
}
