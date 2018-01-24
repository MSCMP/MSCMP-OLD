using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSCMPMessages.Messages {
	public enum MessageIds {
		Handshake,
		Heartbeat,
		HeartbeatResponse,
		Disconnect,
		PlayerSync,
		VehicleSync,
		OpenDoors,
		FullWorldSync,
		AskForWorldState,
		VehicleEnter,
		VehicleLeave,
		PickupObject,
		ReleaseObject,
		PickupableSpawn,
		PickupableDestroy,
		PickupableActivate,
		PickupableSetPosition,
		WorldPeriodicalUpdate,
		RemoveBottle,
		LightSwitch,
	}
}
