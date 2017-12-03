using System.IO;

namespace MSCMP {
	static class Logger {
		/// <summary>
		/// The file used for logging.
		/// </summary>
		static StreamWriter logFile = new StreamWriter(Client.GetPath("clientLog.txt"), false);

		/// <summary>
		/// Set auto flush? (Remember! This is not good for FPS as each write to log is automatically flushing the log file!)
		/// </summary>
		/// <param name="autoFlush"></param>
		public static void SetAutoFlush(bool autoFlush) {
			logFile.AutoFlush = autoFlush;
		}

		/// <summary>
		/// Force flush of the log file.
		/// </summary>
		public static void ForceFlush() {
			logFile.Flush();
		}

		/// <summary>
		/// Write log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Log(string message) {
			logFile.WriteLine(message);
		}

		/// <summary>
		/// Write debug message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Debug(string message) {
#if !PUBLIC_RELEASE
			logFile.WriteLine(message);
#endif
		}
	}
}
