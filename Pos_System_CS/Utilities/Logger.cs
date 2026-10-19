using System;
using System.IO;
using System.Text;

namespace Pos_System_CS
{
    internal static class Logger
    {
        private static readonly object _sync = new object();
        private static readonly string _logFilePath;

        static Logger()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "Pos_System_CS");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                dir = AppDomain.CurrentDomain.BaseDirectory;
            }

            _logFilePath = Path.Combine(dir, "pos_system.log");
        }

        public static string GetLogFilePath()
        {
            return _logFilePath;
        }

        public static void LogError(string message, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[ERROR] " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine(message ?? string.Empty);
                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }
                sb.AppendLine(new string('-', 80));

                lock (_sync)
                {
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
            }
            catch
            {
                // Swallow any logging exceptions to avoid interfering with application flow
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                var line = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
                lock (_sync)
                {
                    File.AppendAllText(_logFilePath, line);
                }
            }
            catch
            {
            }
        }
    }
}
