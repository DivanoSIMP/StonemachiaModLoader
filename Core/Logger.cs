using System.IO;

namespace StonemachiaModLoader.Core
{
    public static class Logger
    {
        private static readonly object Sync = new object();

        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "StonemachiaModLoader",
            "Logs");

        private static readonly string SessionId =
            DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        private static readonly string SessionLogPath =
            Path.Combine(LogDirectory, $"{SessionId}.log");

        private static readonly string LatestLogPath =
        Path.Combine(LogDirectory, "latest.log");

        public static string LogPath => SessionLogPath;

        public static void Info(string message) =>
            Write("INFO", message);

        public static void Warning(string message) =>
            Write("WARN", message);

        public static void Error(string message, Exception? exception = null)
        {
            var details = exception is null
                ? message
                : $"{message}{Environment.NewLine}{exception}";

            Write("ERROR", details);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(LogDirectory);

                    var entry =
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " +
                        $"[{level}] [PID:{Environment.ProcessId}] " +
                        $"{message}{Environment.NewLine}";

                    File.AppendAllText(SessionLogPath, entry);
                    File.AppendAllText(LatestLogPath, entry);
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }
    
        public static void CleanLatestLog()
        {
            if (!File.Exists(LatestLogPath)) return;

            File.Delete(LatestLogPath);
        }
    }
}
