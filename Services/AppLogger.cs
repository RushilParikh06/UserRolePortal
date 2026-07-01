using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace UserRolePortal.Services
{
    public static class AppLogger
    {
        private static readonly string _logDirectory;
        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static readonly SemaphoreSlim _signal = new(0);
        private static readonly Task _writerTask;

        static AppLogger()
        {
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            // Start background writer task
            _writerTask = Task.Run(ProcessQueueAsync);
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
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            _logQueue.Enqueue(logEntry);
            _signal.Release();
        }

        private static async Task ProcessQueueAsync()
        {
            while (true)
            {
                await _signal.WaitAsync(); // Wait until there is a log in the queue
                if (_logQueue.TryDequeue(out var logEntry))
                {
                    try
                    {
                        string filePath = GetLogFilePath();
                        // Asynchronously append to file, preventing thread blocking
                        await File.AppendAllTextAsync(filePath, logEntry);
                    }
                    catch
                    {
                        // Fail silently for logging
                    }
                }
            }
        }
    }
}
