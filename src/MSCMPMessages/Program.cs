namespace MSCMPMessages {
	class Program {
		static void Main(string[] args) {
			Generator generator = new Generator(@"..\..\src\MSCMPClient\Network\NetMessages.generated.cs");


			generator.GenerateMessage(typeof(Messages.QuaternionMessage));
			generator.GenerateMessage(typeof(Messages.Vector3Message));

			generator.GenerateMessage(typeof(Messages.HandshakeMessage));
			generator.GenerateMessage(typeof(Messages.HeartbeatMessage));
			generator.GenerateMessage(typeof(Messages.HeartbeatResponseMessage));
			generator.GenerateMessage(typeof(Messages.DisconnectMessage));
			generator.GenerateMessage(typeof(Messages.PlayerSyncMessage));
			generator.GenerateMessage(typeof(Messages.OpenDoorsMessage));

			generator.GenerateMessage(typeof(Messages.AskForWorldStateMessage));
			generator.GenerateMessage(typeof(Messages.FullWorldSyncMessage));

			generator.EndGeneration();
		}
	}
}
