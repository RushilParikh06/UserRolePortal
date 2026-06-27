using System;
using System.IO;

namespace UserRolePortal.Services
{
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _logDirectory;

        static AppLogger()
        {
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static string GetLogFilePath()
        {
            return Path.Combine(_logDirectory, $"app-log-{DateTime.Now:yyyy-MM-dd}.txt");
        }

        public static void LogActivity(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string logMsg = message;
            if (ex != null)
            {
                logMsg += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
            WriteLog("ERROR", logMsg);
        }

        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    string filePath = GetLogFilePath();
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(filePath, logEntry);
                }
                catch
                {
                    // Fail silently for logging
                }
            }
        }
    }
}
