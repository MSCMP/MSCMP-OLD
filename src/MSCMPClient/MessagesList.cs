using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MSCMP {
	public enum MessageSeverity { Info, Error }

	/// <summary>
	/// Basic message hud.
	/// </summary>
	class MessagesList {

		const int MESSAGES_COUNT = 5;
		static Color[] colors = new Color[MESSAGES_COUNT];
		static string[] messages = new string[MESSAGES_COUNT];

		/// <summary>
		/// Add message to the hud.
		/// </summary>
		/// <param name="message"></param>

		public static void AddMessage(string message, MessageSeverity severity) {

			for (int i = 1; i < MESSAGES_COUNT; ++i) {
				colors[i - 1] = colors[i];
				messages[i - 1] = messages[i];
			}

			messages[MESSAGES_COUNT - 1] = message;
			Color color = Color.white;
			switch (severity) {
			case MessageSeverity.Info: color = Color.white; break;

			case MessageSeverity.Error: color = Color.red; break;
			}

			colors[MESSAGES_COUNT - 1] = color;
		}

		/// <summary>
		/// Clear chat.
		/// </summary>
		public static void ClearMessages() {
			for (int i = 0; i < MESSAGES_COUNT; ++i) { messages[i] = ""; }
		}

		/// <summary>
		/// Draw message list.
		/// </summary>

		public static void Draw() {
			float x = 10.0f;
			float y = Screen.height / 2.0f;
			const float lineWidth = 500;
			const float lineHeight = 20;
			for (int i = 0; i < MESSAGES_COUNT; ++i) {
				if (messages[i] != null && messages[i].Length > 0) {
					GUI.color = Color.black;
					GUI.Label(new Rect(x + 1, y + 1, lineWidth, lineHeight), messages[i]);

					GUI.color = colors[i];
					GUI.Label(new Rect(x, y, lineWidth, lineHeight), messages[i]);
				}
				y += lineHeight;
			}
		}
	}
}
