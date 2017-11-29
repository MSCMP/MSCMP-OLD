using System.IO;

namespace MSCMP.Network {
	public interface INetMessage {


		byte MessageId {
			get;
		}

		bool Read(BinaryReader reader);
		bool Write(BinaryWriter reader);
	}
}
