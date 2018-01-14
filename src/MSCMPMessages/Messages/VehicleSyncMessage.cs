using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.VehicleSync)]
	class VehicleSyncMessage {

		Vector3Message position;
		QuaternionMessage rotation;

		float steering;

	}
}
