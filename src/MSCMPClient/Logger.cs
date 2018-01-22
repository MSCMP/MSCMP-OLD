using System.IO;

namespace MSCMP {
	static class Logger {

		/// <summary>
		/// The file used for logging.
		/// </summary>
		static StreamWriter logFile = null;


		/// <summary>
		/// Setup logger.
		/// </summary>
		/// <param name="logPath">The path where log file should be created</param>
		/// <returns></returns>
		public static bool SetupLogger(string logPath) {
			try {
				logFile = new StreamWriter(logPath, false);
			}
			catch {
				// Unfortunately there is no place where we could send the failure.
				return false;
			}
			return logFile != null;
		}

		/// <summary>
		/// Set auto flush? (Remember! This is not good for FPS as each write to log is automatically flushing the log file!)
		/// </summary>
		/// <param name="autoFlush"></param>
		public static void SetAutoFlush(bool autoFlush) {
			if (logFile != null) {
				logFile.AutoFlush = autoFlush;
			}
		}

		/// <summary>
		/// Force flush of the log file.
		/// </summary>
		public static void ForceFlush() {
			if (logFile != null) {
				logFile.Flush();
			}
		}

		/// <summary>
		/// Write log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Log(string message) {
			if (logFile != null) {
				logFile.WriteLine(message);
			}
		}

		/// <summary>
		/// Write log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Warning(string message) {
			Log("[WARN] " + message);
		}

		/// <summary>
		/// Write debug message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Debug(string message) {
#if !PUBLIC_RELEASE
			Log("[DEBUG] " + message);
#endif
		}
	}
}
