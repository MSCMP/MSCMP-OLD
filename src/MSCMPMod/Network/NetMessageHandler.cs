using System.Collections.Generic;
using System.IO;

namespace MSCMP.Network {
	/// <summary>
	/// Network message handler.
	/// </summary>
	class NetMessageHandler {

		delegate void HandleMessageLowLevel(
				Steamworks.CSteamID sender, BinaryReader reader);
		private Dictionary<byte, HandleMessageLowLevel> messageHandlers =
				new Dictionary<byte, HandleMessageLowLevel>();

		/// <summary>
		/// Delegate type for network messages handler.
		/// </summary>
		/// <typeparam name="T">The type of the network message.</typeparam>
		/// <param name="sender">Steam id that sent us the message.</param>
		/// <param name="message">The deserialized image.</param>
		public delegate void MessageHandler<T>(Steamworks.CSteamID sender, T message);

		/// <summary>
		/// Network manager owning this handler.
		/// </summary>
		NetManager netManager = null;

		public NetMessageHandler(NetManager theNetManager) {
			netManager = theNetManager;
		}

		/// <summary>
		/// Binds handler for the given message. (There can be only one handler per
		/// message)
		/// </summary>
		/// <typeparam name="T">The type of message to register handler for.</typeparam>
		/// <param name="Handler">The handler lambda.</param>
		public void BindMessageHandler<T>(MessageHandler<T> Handler)
				where T : INetMessage, new() {
			T message = new T();

			messageHandlers.Add(
					message.MessageId, (Steamworks.CSteamID sender, BinaryReader reader) => {
						if (!message.Read(reader)) {
							Logger.Log("Failed to read network message " + message.MessageId +
									" received from " + sender.ToString());
							return;
						}
						Handler(sender, message);
					});
		}

		/// <summary>
		/// Process incoming network message.
		/// </summary>
		/// <param name="messageId">The id of the message.</param>
		/// <param name="senderSteamId">Steamid of the sender client.</param>
		/// <param name="reader">The binary reader contaning message data.</param>
		public void ProcessMessage(
				byte messageId, Steamworks.CSteamID senderSteamId, BinaryReader reader) {
			if (messageHandlers.ContainsKey(messageId)) {
				messageHandlers[messageId](senderSteamId, reader);
			}
		}
	}
}
