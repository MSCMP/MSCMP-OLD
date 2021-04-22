using System.IO;

namespace MSCMP.Network {
	/// <summary>
	/// Base class of the network message.
	/// </summary>
	public interface INetMessage {
		/// <summary>
		/// The unique id of the network message.
		/// </summary>
		byte MessageId { get; }

		/// <summary>
		/// Deserialize network message from binary buffer.
		/// </summary>
		/// <param name="reader">The reader containing serialized message.</param>
		/// <returns>true if message was consistent with the definition and it can be
		/// handled by client, false otherwise</returns>
		bool Read(BinaryReader reader);

		/// <summary>
		/// Serialize network message into the binary buffer.
		/// </summary>
		/// <param name="writer">The writer to which the message should be
		/// written.</param> <returns>true if everything properly wrote and message can
		/// be send, false otherwise</returns>
		bool Write(BinaryWriter writer);
	}
}
