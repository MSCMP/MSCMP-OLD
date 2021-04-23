using System;
using System.IO;
using MSCLoader;
using MSCMP.Utilities;

namespace MSCMP {
	public static class Logger {

		private const string LOG_FILE_NAME = "MSCMPClientLog.txt";

		/// <summary>
		/// The file used for logging.
		/// </summary>
		private static StreamWriter _logFile = null;
		private static bool _isInitialized = false;
		
		public static bool IsInitialized => _isInitialized;

		/// <summary>
		/// Setup logger.
		/// </summary>
		/// <param name="logPath">The path where log file should be created</param>
		/// <returns></returns>
		public static void SetupLogger() {
			var logPath = GetLogPath();
			try {
				_logFile = new StreamWriter(logPath) {AutoFlush = true};
				_isInitialized = true;
			} catch (Exception exception){
				ModConsole.Error($"Cannot create log file because of error: {exception}");
				_isInitialized = false;
			}
		}

		private static string GetLogPath() {
			var logPath = Path.Combine(ModUtils.GetGamePath(), LOG_FILE_NAME);
			return logPath;
		}

		/// <summary>
		/// Write log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Log(string message) {
			WriteToLogFile(message);
			ModConsole.Print(message);
		}

		/// <summary>
		/// Write warning log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Warning(string message) {
			_logFile?.WriteLine($"Warning: {message}");
			ModConsole.Warning(message);
		}

		/// <summary>
		/// Write error log message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Error(string message) {
			_logFile?.WriteLine($"Error: {message}");
			ModConsole.Error(message);
		}
		
		public static void Error(string message, Exception exception) {
			_logFile?.WriteLine($"Error: {message}");
			_logFile?.WriteLine(exception);
			ModConsole.Error(message);
			ModConsole.Error(exception.ToString());
		}

		private static void WriteToLogFile(string message) {
			_logFile?.WriteLine(message);
		}
		
		/// <summary>
		/// Write debug message.
		/// </summary>
		/// <param name="message">Message to write.</param>
		public static void Debug(string message) {
#if !PUBLIC_RELEASE
			var debugMessage = $"[DEBUG] {message}";
			ModConsole.Print(debugMessage);
			_logFile?.WriteLine(debugMessage);
#endif
		}
	}
}
