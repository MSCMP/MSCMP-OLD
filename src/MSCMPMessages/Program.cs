namespace MSCMPMessages {
	class Program {
		static void Main(string[] args) {
			Generator generator = new Generator(@"..\..\src\MSCMPClient\Network\NetMessages.generated.cs");

			generator.GenerateMessage(typeof(Messages.QuaternionMessage));
			generator.GenerateMessage(typeof(Messages.Vector3Message));
			generator.GenerateMessage(typeof(Messages.HandshakeMessage));
			generator.GenerateMessage(typeof(Messages.PlayerSyncMessage));

			generator.EndGeneration();
		}
	}
}
