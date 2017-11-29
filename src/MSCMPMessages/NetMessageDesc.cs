using System;


namespace MSCMPMessages {
	class NetMessageDesc : Attribute {

		public Messages.MessageIds messageId;

		public NetMessageDesc(Messages.MessageIds id) {
			this.messageId = id;
		}
	}
}
