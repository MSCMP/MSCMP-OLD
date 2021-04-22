using UnityEngine;
using MSCMP.Utilities;

namespace MSCMP.Network {
	/// <summary>
	/// Handles network statistics.
	/// </summary>
	class NetStatistics {

		const int HISTORY_SIZE = 100;
		long[] bytesReceivedHistory = new long[HISTORY_SIZE];
		long[] bytesSentHistory = new long[HISTORY_SIZE];
		long maxBytesReceivedInHistory = 0;
		long maxBytesSentInHistory = 0;

		int packetsSendTotal = 0;
		int packetsReceivedTotal = 0;

		int packetsSendLastFrame = 0;
		int packetsReceivedLastFrame = 0;

		int packetsSendCurrentFrame = 0;
		int packetsReceivedCurrentFrame = 0;

		long bytesSentTotal = 0;
		long bytesReceivedTotal = 0;

		long bytesSentLastFrame = 0;
		long bytesReceivedLastFrame = 0;

		long bytesSentCurrentFrame = 0;
		long bytesReceivedCurrentFrame = 0;

		/// <summary>
		/// Network manager owning this object.
		/// </summary>
		NetManager netManager = null;

		/// <summary>
		/// Line material used to draw the graph.
		/// </summary>
		Material lineMaterial = null;

		public NetStatistics(NetManager netManager) { this.netManager = netManager; }

		void SetupLineMaterial() {
			if (lineMaterial != null) { return; }

			// Setup graph lines material.

			Shader shader =
					Shader.Find("GUI/Text Shader"); // Text shader is sufficient for this case.
			Client.Assert(shader != null, "Shader not found!");
			lineMaterial = new Material(shader);
			Client.Assert(lineMaterial != null, "Failed to setup material!");
		}

		/// <summary>
		/// Resets all frame statistics.
		/// </summary>
		public void NewFrame() {
			packetsSendLastFrame = packetsSendCurrentFrame;
			packetsReceivedLastFrame = packetsReceivedCurrentFrame;

			packetsSendCurrentFrame = 0;
			packetsReceivedCurrentFrame = 0;

			bytesSentLastFrame = bytesSentCurrentFrame;
			bytesReceivedLastFrame = bytesReceivedCurrentFrame;

			maxBytesReceivedInHistory = bytesSentCurrentFrame;
			maxBytesSentInHistory = bytesReceivedCurrentFrame;
			for (int i = 0; i < HISTORY_SIZE - 1; ++i) {
				bytesSentHistory[i] = bytesSentHistory[i + 1];
				if (maxBytesSentInHistory < bytesSentHistory[i]) {
					maxBytesSentInHistory = bytesSentHistory[i];
				}
				bytesReceivedHistory[i] = bytesReceivedHistory[i + 1];
				if (maxBytesReceivedInHistory < bytesReceivedHistory[i]) {
					maxBytesReceivedInHistory = bytesReceivedHistory[i];
				}
			}

			bytesSentHistory[HISTORY_SIZE - 1] = bytesSentCurrentFrame;
			bytesReceivedHistory[HISTORY_SIZE - 1] = bytesReceivedCurrentFrame;

			bytesSentCurrentFrame = 0;
			bytesReceivedCurrentFrame = 0;
		}

		/// <summary>
		/// Records new send message.
		/// </summary>
		/// <param name="messageId">The received message id.</param>
		/// <param name="bytes">Received bytes.</param>
		public void RecordSendMessage(int messageId, long bytes) {
			bytesSentCurrentFrame += bytes;
			bytesSentTotal += bytes;

			packetsSendCurrentFrame++;
			packetsSendTotal++;
		}

		/// <summary>
		/// Records new received message.
		/// </summary>
		/// <param name="messageId">The received message id.</param>
		/// <param name="bytes">Received bytes.</param>
		public void RecordReceivedMessage(int messageId, long bytes) {
			bytesReceivedCurrentFrame += bytes;
			bytesReceivedTotal += bytes;

			packetsReceivedCurrentFrame++;
			packetsReceivedTotal++;
		}

		/// <summary>
		/// Draws statistic label.
		/// </summary>
		/// <remarks>GUI color after this call may not be white!</remarks>
		/// <param name="name">Name of the statistic.</param>
		/// <param name="value">The statistic value.</param>
		/// <param name="critical">The critical statistic value to highlight. (if -1
		/// there is no critical value)</param> <param name="bytes">Is the stat
		/// representing bytes?</param>
		void DrawStatHelper(ref Rect rct, string name, long value, int critical = -1,
				bool bytes = false) {
			GUI.color = Color.white;
			GUI.Label(rct, name);

			bool isCriticalValue = (critical != -1 && value >= critical);
			GUI.color = isCriticalValue ? Color.red : Color.white;

			rct.x += rct.width;
			if (bytes) {
				GUI.Label(rct, FormatBytes(value));
			} else {
				GUI.Label(rct, value.ToString());
			}

			rct.x -= rct.width;
			rct.y += rct.height;
		}

		/// <summary>
		/// Draws text label.
		/// </summary>
		/// <remarks>GUI color after this call may not be white!</remarks>
		/// <param name="name">Name of the statistic.</param>
		/// <param name="text">The text value.</param>
		void DrawTextHelper(ref Rect rct, string name, string text) {
			GUI.color = Color.white;
			GUI.Label(rct, name);
			rct.x += rct.width;
			GUI.Label(rct, text);

			rct.x -= rct.width;
			rct.y += rct.height;
		}

		/// <summary>
		/// Draw line using GL.
		/// </summary>
		/// <param name="start">Line start position.</param>
		/// <param name="end">Line end position.</param>
		/// <param name="color">Line color.</param>
		void DrawLineHelper(Vector2 start, Vector2 end, Color color) {
			GL.Color(color);
			GL.Vertex3(start.x, Screen.height - start.y, 0.0f);
			GL.Vertex3(end.x, Screen.height - end.y, 0.0f);
		}

		/// <summary>
		/// Draw network graph.
		/// </summary>
		/// <param name="drawRect">Rectangle where graph should drawn.</param>
		void DrawGraph(Rect drawRect) {
			SetupLineMaterial();

			lineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.LoadPixelMatrix();
			GL.Begin(GL.LINES);

			// draw graph boundaries

			DrawLineHelper(new Vector2(drawRect.x, drawRect.y),
					new Vector2(drawRect.x + drawRect.width, drawRect.y), Color.gray);
			DrawLineHelper(new Vector2(drawRect.x, drawRect.y),
					new Vector2(drawRect.x, drawRect.y + drawRect.height), Color.gray);
			DrawLineHelper(new Vector2(drawRect.x + drawRect.width, drawRect.y),
					new Vector2(drawRect.x + drawRect.width, drawRect.y + drawRect.height),
					Color.gray);
			DrawLineHelper(new Vector2(drawRect.x, drawRect.y + drawRect.height),
					new Vector2(drawRect.x + drawRect.width, drawRect.y + drawRect.height),
					Color.gray);

			float stepWidth = drawRect.width / HISTORY_SIZE;

			for (int i = 0; i < HISTORY_SIZE; ++i) {
				// draw send

				long previousHistoryValue = i > 0 ? bytesSentHistory[i - 1] : 0;
				float previousY = drawRect.y +
						drawRect.height *
								Mathf.Clamp01(1.0f -
										((float)previousHistoryValue /
												Mathf.Max(1, maxBytesSentInHistory)));
				var start =
						new Vector2(drawRect.x + stepWidth * Mathf.Max(i - 1, 0), previousY);
				float currentY = drawRect.y +
						drawRect.height *
								Mathf.Clamp01(1.0f -
										((float)bytesSentHistory[i] /
												Mathf.Max(1, maxBytesSentInHistory)));
				var end = new Vector2(drawRect.x + stepWidth * i, currentY);
				DrawLineHelper(start, end, Color.red);

				// draw receive

				previousHistoryValue = i > 0 ? bytesReceivedHistory[i - 1] : 0;
				previousY = drawRect.y +
						drawRect.height *
								Mathf.Clamp01(1.0f -
										((float)previousHistoryValue /
												Mathf.Max(1, maxBytesReceivedInHistory)));
				start = new Vector2(drawRect.x + stepWidth * Mathf.Max(i - 1, 0), previousY);
				currentY = drawRect.y +
						drawRect.height *
								Mathf.Clamp01(1.0f -
										((float)bytesReceivedHistory[i] /
												Mathf.Max(1, maxBytesReceivedInHistory)));
				end = new Vector2(drawRect.x + stepWidth * i, currentY);
				DrawLineHelper(start, end, Color.green);
			}

			GL.End();
			GL.PopMatrix();
		}

		/// <summary>
		/// Helper used to format bytes.
		/// </summary>
		/// <param name="bytes">The bytes.</param>
		/// <returns>Formatted bytes string.</returns>
		string FormatBytes(long bytes) {
			if (bytes >= 1024 * 1024) {
				float mb = ((float)bytes / (1024 * 1024));
				return mb.ToString("0.00") + " MB";
			} else if (bytes >= 1024) {
				float kb = ((float)bytes / 1024);
				return kb.ToString("0.00") + " KB";
			}
			return bytes.ToString() + " B";
		}

		/// <summary>
		/// Draw network statistics.
		/// </summary>
		public void Draw() {
			GUI.color = Color.white;
			const int WINDOW_WIDTH = 300;
			const int WINDOW_HEIGHT = 600;
			Rect statsWindowRect = new Rect(Screen.width - WINDOW_WIDTH - 10,
					Screen.height - WINDOW_HEIGHT - 10, WINDOW_WIDTH, WINDOW_HEIGHT);
			GUI.Window(666, statsWindowRect, (int window) => {
				// Draw traffic graph title.

				var rct = new Rect(10, 20, 200, 25);
				GUI.Label(rct, $"Traffic graph (last {HISTORY_SIZE} frames):");
				rct.y += 25;

				var graphRect = new Rect(rct.x, rct.y, WINDOW_WIDTH - 20, 100);

				// Draw graph background.

				GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.35f);
				IMGUIUtils.DrawPlainColorRect(graphRect);

				// Draw the graph itself.

				graphRect.x += statsWindowRect.x;
				graphRect.y += statsWindowRect.y;
				DrawGraph(graphRect);

				GUI.color = Color.white;
				rct.y += 5;
				rct.x += 5;
				IMGUIUtils.DrawSmallLabel($"{FormatBytes(maxBytesSentInHistory)} sent/frame",
						rct, Color.red, true);
				rct.y += 12;

				IMGUIUtils.DrawSmallLabel(
						$"{FormatBytes(maxBytesReceivedInHistory)} recv/frame", rct, Color.green,
						true);
				rct.y -= 12 - 5;
				rct.x -= 5;

				rct.y += graphRect.height;

				rct.height = 20;

				// Draw separator

				GUI.color = Color.black;
				IMGUIUtils.DrawPlainColorRect(new Rect(0, rct.y, WINDOW_WIDTH, 2));
				rct.y += 2;

				// Draw stats background

				GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
				IMGUIUtils.DrawPlainColorRect(
						new Rect(0, rct.y, WINDOW_WIDTH, WINDOW_HEIGHT - rct.y));

				// Draw statistics

				DrawStatHelper(ref rct, "packetsSendTotal", packetsSendTotal);
				DrawStatHelper(ref rct, "packetsReceivedTotal", packetsReceivedTotal);
				DrawStatHelper(ref rct, "packetsSendLastFrame", packetsSendLastFrame, 1000);
				DrawStatHelper(
						ref rct, "packetsReceivedLastFrame", packetsReceivedLastFrame, 1000);
				DrawStatHelper(
						ref rct, "packetsSendCurrentFrame", packetsSendCurrentFrame, 1000);
				DrawStatHelper(ref rct, "packetsReceivedCurrentFrame",
						packetsReceivedCurrentFrame, 1000);
				DrawStatHelper(ref rct, "bytesSendTotal", bytesSentTotal, -1, true);
				DrawStatHelper(ref rct, "bytesReceivedTotal", bytesReceivedTotal, -1, true);
				DrawStatHelper(
						ref rct, "bytesSendLastFrame", bytesSentLastFrame, 1000, true);
				DrawStatHelper(
						ref rct, "bytesReceivedLastFrame", bytesReceivedLastFrame, 1000, true);
				DrawStatHelper(
						ref rct, "bytesSendCurrentFrame", bytesSentCurrentFrame, 1000, true);
				DrawStatHelper(ref rct, "bytesReceivedCurrentFrame",
						bytesReceivedCurrentFrame, 1000, true);

				// Draw separator

				rct.y += 2;
				GUI.color = Color.black;
				IMGUIUtils.DrawPlainColorRect(new Rect(0, rct.y, WINDOW_WIDTH, 2));
				rct.y += 2;

				// Draw P2P session state.

				DrawTextHelper(ref rct, "Steam session state:", "");

				Steamworks.P2PSessionState_t sessionState =
						new Steamworks.P2PSessionState_t();
				if (netManager.GetP2PSessionState(out sessionState)) {
					DrawTextHelper(
							ref rct, "Is Connecting", sessionState.m_bConnecting.ToString());
					DrawTextHelper(ref rct, "Is connection active",
							sessionState.m_bConnectionActive == 0 ? "no" : "yes");
					DrawTextHelper(ref rct, "Using relay?",
							sessionState.m_bConnectionActive == 0 ? "no" : "yes");
					DrawTextHelper(ref rct, "Session error",
							Utils.P2PSessionErrorToString(
									(Steamworks.EP2PSessionError)sessionState.m_eP2PSessionError));
					DrawTextHelper(ref rct, "Bytes queued for send",
							FormatBytes(sessionState.m_nBytesQueuedForSend));
					DrawTextHelper(ref rct, "Packets queued for send",
							sessionState.m_nPacketsQueuedForSend.ToString());
					uint uip = sessionState.m_nRemoteIP;
					string ip = string.Format("{0}.{1}.{2}.{3}", (uip >> 24) & 0xff,
							(uip >> 16) & 0xff, (uip >> 8) & 0xff, uip & 0xff);
					DrawTextHelper(ref rct, "Remote ip", ip);
					DrawTextHelper(
							ref rct, "Remote port", sessionState.m_nRemotePort.ToString());
				} else {
					DrawTextHelper(ref rct, "Session inactive.", "");
				}
			}, "Network statistics");
		}
	}
}
