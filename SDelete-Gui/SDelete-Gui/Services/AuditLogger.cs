using System;
using System.IO;
using System.Threading;

namespace SDelete_Gui.Services
{
    /// <summary>
    /// Writes timestamped audit records for completed SDelete operations.
    /// </summary>
    public static class AuditLogger
    {
        private const string LogMutexName = @"Local\SDeleteGui_AuditLog_Mutex";

        /// <summary>
        /// Formats a single audit record with an ISO-8601 timestamp.
        /// </summary>
        public static string FormatEntry(
            DateTimeOffset timestamp,
            SDeleteMode mode,
            int passes,
            string targetPath,
            int exitCode,
            string errorDetail = null)
        {
            string status = exitCode == 0 ? "OK" : "ERROR";
            string isoTime = timestamp.ToString("yyyy-MM-ddTHH:mm:sszzz");
            string modeToken = SDeleteCommandBuilder.ToCliToken(mode);
            string baseLine = $"{isoTime} | STATUS={status} | MODE={modeToken} | PASSES={passes} | EXIT={exitCode} | TARGET={targetPath}";

            if (!string.IsNullOrWhiteSpace(errorDetail))
            {
                string sanitized = errorDetail.Replace("\r", " ").Replace("\n", " ").Trim();
                return $"{baseLine} | DETAIL={sanitized}";
            }

            return baseLine;
        }

        /// <summary>
        /// Appends a formatted entry to the log file under a cross-process mutex.
        /// </summary>
        public static void AppendEntry(
            string logFilePath,
            SDeleteMode mode,
            int passes,
            string targetPath,
            int exitCode,
            string errorDetail = null)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string line = FormatEntry(DateTimeOffset.Now, mode, passes, targetPath, exitCode, errorDetail);

            using (var mutex = new Mutex(false, LogMutexName))
            {
                bool acquired = false;
                try
                {
                    acquired = mutex.WaitOne(TimeSpan.FromSeconds(5));
                    File.AppendAllText(logFilePath, line + Environment.NewLine);
                }
                finally
                {
                    if (acquired)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }
    }
}
