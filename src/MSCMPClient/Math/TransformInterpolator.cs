using UnityEngine;

namespace MSCMP.Math {
	class TransformInterpolator {
		QuaternionInterpolator rotation = new QuaternionInterpolator();
		Vector3Interpolator position = new Vector3Interpolator();

		public Vector3 CurrentPosition {
			get { return position.Current; }
		}

		public Quaternion CurrentRotation {
			get { return rotation.Current; }
		}

		public void Teleport(Vector3 pos, Quaternion rot) {
			position.Teleport(pos);
			rotation.Teleport(rot);
		}

		public void SetTarget(Vector3 pos, Quaternion rot) {
			position.SetTarget(pos);
			rotation.SetTarget(rot);
		}

		public void Evaluate(ref Vector3 pos, ref Quaternion rot, float alpha) {
			pos = position.Evaluate(alpha);
			rot = rotation.Evaluate(alpha);
		}

		public void Evaluate(float alpha) {
			position.Evaluate(alpha);
			rotation.Evaluate(alpha);
		}
	}
}
