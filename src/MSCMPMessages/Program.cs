namespace MSCMPMessages {
	class Program {
		static void Main(string[] args) {
			Generator generator = new Generator(@"..\..\src\MSCMPClient\Network\NetMessages.generated.cs");

			generator.GenerateMessage(typeof(Messages.QuaternionMessage));
			generator.GenerateMessage(typeof(Messages.Vector3Message));
			generator.GenerateMessage(typeof(Messages.TransformMessage));
			generator.GenerateMessage(typeof(Messages.HandshakeMessage));
			generator.GenerateMessage(typeof(Messages.HeartbeatMessage));
			generator.GenerateMessage(typeof(Messages.HeartbeatResponseMessage));
			generator.GenerateMessage(typeof(Messages.DisconnectMessage));
			generator.GenerateMessage(typeof(Messages.PickedUpSync));
			generator.GenerateMessage(typeof(Messages.PlayerSyncMessage));
			generator.GenerateMessage(typeof(Messages.OpenDoorsMessage));
			generator.GenerateMessage(typeof(Messages.AskForWorldStateMessage));
			generator.GenerateMessage(typeof(Messages.DoorsInitMessage));
			generator.GenerateMessage(typeof(Messages.VehicleInitMessage));
			generator.GenerateMessage(typeof(Messages.PickupableSpawnMessage));
			generator.GenerateMessage(typeof(Messages.PickupableDestroyMessage));
			generator.GenerateMessage(typeof(Messages.FullWorldSyncMessage));
			generator.GenerateMessage(typeof(Messages.VehicleEnterMessage));
			generator.GenerateMessage(typeof(Messages.VehicleLeaveMessage));
			generator.GenerateMessage(typeof(Messages.VehicleSyncMessage));
			generator.GenerateMessage(typeof(Messages.PickupObjectMessage));
			generator.GenerateMessage(typeof(Messages.ReleaseObjectMessage));
			generator.EndGeneration();
		}
	}
}
