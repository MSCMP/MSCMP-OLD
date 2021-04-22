using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSCMP.UI {

	/// <summary>
	/// Console ui element.
	/// </summary>
	class Console {

		/// <summary>
		/// The command delegate.
		/// </summary>
		/// <param name="args">The arguments - first one will be name of the
		/// command.</param>
		public delegate void CommandDelegate(string[] args);

		private static Dictionary<string, CommandDelegate> Commands =
				new Dictionary<string, CommandDelegate>();

		/// <summary>
		/// Register new console command.
		/// </summary>
		/// <param name="command">Command to register.</param>
		/// <param name="commandDelegate">The command delegate.</param>
		public static void RegisterCommand(
				string command, CommandDelegate commandDelegate) {
			Commands.Add(command, commandDelegate);
		}

		/// <summary>
		/// Execute given command.
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <returns>true if command was executed, false otherwise</returns>
		public static bool ExecuteCommand(string command) {
			try {
				string[] args = command.Split(' ');
				if (args.Length == 0) { return false; }

				var commandDelegate = Commands[args[0]];
				if (commandDelegate != null) {
					commandDelegate.Invoke(args);
					return true;
				}
			} catch (Exception e) {
				Client.ConsoleMessage($"COMMAND ERROR: {e}");
				return true; // True, so it won't say Invalid Command
			}
			return false;
		}

		/// <summary>
		/// Is the console visible?
		/// </summary>
		bool isVisible = false;

		/// <summary>
		/// Should console input field be focused next frame?
		/// </summary>
		bool focusConsole = false;

		/// <summary>
		/// Current console input text.
		/// </summary>
		string inputText = "";

		/// <summary>
		/// List of all messages in console.
		/// </summary>
		List<string> messages = new List<string>();

		/// <summary>
		/// The console singleton.
		/// </summary>
		static Console instance = null;

		/// <summary>
		/// Get currently active instance of console.
		/// </summary>
		static public Console Instance {
			get { return instance; }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public Console() { instance = this; }

		/// <summary>
		/// Destructor.
		/// </summary>
		~Console() { instance = null; }

		/// <summary>
		/// The input history.
		/// </summary>
		List<string> inputHistory = new List<string>();

		/// <summary>
		/// Current input history index, if -1 no input history entry is being used.
		/// </summary>
		int currentHistoryEntryIndex = -1;

		/// <summary>
		/// Handle the input user typed.
		/// </summary>
		void HandleInput() {
			if (!ExecuteCommand(inputText)) {
				AddMessage($"ERROR: Unknown command {inputText}.");
			}
			inputHistory.Add(inputText);
			inputText = string.Empty;
			currentHistoryEntryIndex = -1;
		}

		/// <summary>
		/// Current console rectangle.
		/// </summary>
		Rect consoleRect = new Rect(5, 5, 800, 400);

		/// <summary>
		/// The width of the console button.
		/// </summary>
		const int BUTTON_WIDTH = 80;

		/// <summary>
		/// Draw console.
		/// </summary>
		public void Draw() {
			HandleEvent();

			if (!isVisible) { return; }

			GUI.color = Color.white;
			consoleRect =
					GUI.Window(69, consoleRect, DrawConsole, "CONSOLE (Press ~ to hide)");
		}

		/// <summary>
		/// Handle input event.
		/// </summary>
		private void HandleEvent() {
			if (Event.current.rawType != EventType.KeyUp) { return; }

			switch (Event.current.keyCode) {
			case KeyCode.BackQuote:
				isVisible = !isVisible;
				if (isVisible) { focusConsole = true; }
				break;

			case KeyCode.Return:
				if (isVisible) { HandleInput(); }
				break;

			case KeyCode.UpArrow:
				if (isVisible) { CycleThroughInputHistory(false); }
				break;

			case KeyCode.DownArrow:
				if (isVisible) { CycleThroughInputHistory(true); }
				break;
			}
		}

		/// <summary>
		/// Cycles through input history.
		/// </summary>
		/// <param name="forward">Should cycle forward or backwards?</param>
		void CycleThroughInputHistory(bool forward) {
			if (forward) {
				++currentHistoryEntryIndex;
				if (currentHistoryEntryIndex == inputHistory.Count) {
					currentHistoryEntryIndex = -1;
				}
			} else {
				if (currentHistoryEntryIndex == -1) {
					if (inputHistory.Count > 0) {
						currentHistoryEntryIndex = inputHistory.Count - 1;
					}
				} else {
					--currentHistoryEntryIndex;
				}
			}

			if (currentHistoryEntryIndex == -1) {
				inputText = "";
			} else {
				inputText = inputHistory[currentHistoryEntryIndex];
			}

			// Moving the cursor to the last character
			var editor = GUIUtility.GetStateObject(
					typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
			editor.selectPos = inputText.Length + 1;
			editor.pos = inputText.Length + 1;
		}

		/// <summary>
		/// Current console scroll position.
		/// </summary>
		Vector2 scrollPosition = new Vector2();

		/// <summary>
		/// Maximum visible messages.
		/// </summary>
		const int MAX_VISIBLE_MESSAGES = 1000;

		/// <summary>
		/// The height of single message line.
		/// </summary>
		const float MESSAGE_HEIGHT = 20.0f;

		void DrawConsole(int windowId) {
			// Draw messages.
			var scrollViewRect =
					new Rect(10, 20, consoleRect.width - 20, consoleRect.height - 55);
			int visibleMessagesCount = Mathf.Min(messages.Count, MAX_VISIBLE_MESSAGES);
			var viewRect = new Rect(
					0, 0, scrollViewRect.width - 50, visibleMessagesCount * MESSAGE_HEIGHT);

			var messageRect = viewRect;
			messageRect.height = MESSAGE_HEIGHT;
			messageRect.y = viewRect.height - MESSAGE_HEIGHT;

			scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);

			for (int i = messages.Count; i > (messages.Count - visibleMessagesCount);
					 --i) {
				GUI.Label(messageRect, messages[i - 1]);
				messageRect.y -= messageRect.height;
			}

			GUI.EndScrollView();

			// Draw input field.

			int inputWidth = (int)(consoleRect.width - BUTTON_WIDTH * 2 - 30 - 10);
			var inputRect = new Rect(10, consoleRect.height - 30, inputWidth, 20);
			GUI.SetNextControlName("ConsoleTextField");
			inputText = GUI.TextField(inputRect, inputText);

			// Draw send button.

			inputRect.x += inputWidth + 10;
			inputRect.width = BUTTON_WIDTH;

			if (GUI.Button(inputRect, "SEND")) { HandleInput(); }

			inputRect.x += BUTTON_WIDTH + 10;
			if (GUI.Button(inputRect, "CLEAR")) { Clear(); }

			if (focusConsole) { GUI.FocusControl("ConsoleTextField"); }

			// Make console dragable.
			// Must be called as last otherwise different IMGUI calls will have broken
			// state.

			GUI.DragWindow();
		}

		/// <summary>
		/// Add new message to the console.
		/// </summary>
		/// <param name="message">The message to add.</param>
		public void AddMessage(string message) {
			scrollPosition.y += MESSAGE_HEIGHT;
			messages.Add(message);
		}

		/// <summary>
		/// Clears console.
		/// </summary>
		public void Clear() {
			messages.Clear();
			scrollPosition.y = 0.0f;
		}
	}
}
