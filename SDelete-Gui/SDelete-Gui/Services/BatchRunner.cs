using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SDelete_Gui.Properties;

namespace SDelete_Gui.Services
{
    /// <summary>
    /// Collects multi-selected Explorer targets, asks for confirmation, runs SDelete, and writes audit logs.
    /// </summary>
    public static class BatchRunner
    {
        private const int BatchDebounceMilliseconds = 350;
        private const int MaxBatchWaitMilliseconds = 2500;
        private const int MaxTargetsShownInPrompt = 10;

        /// <summary>
        /// Executes a runner request from Windows Explorer context menus.
        /// </summary>
        public static int Execute(RunnerRequest request, string sdeleteExePath)
        {
            SDeleteOptions options = SDeleteOptions.Load();
            List<string> targets = CollectBatchTargets(request, options.SequentialExecution);

            // A secondary instance forwarded its target to the primary collector and exits now.
            if (targets == null || targets.Count == 0)
            {
                return 0;
            }

            if (options.ConfirmBeforeDelete && !ConfirmExecution(request, targets))
            {
                return 0;
            }

            if (!File.Exists(sdeleteExePath))
            {
                MessageBox.Show(
                    string.Format(Messages.SDeleteNotFoundAndNotDownloadable, Path.GetDirectoryName(sdeleteExePath)),
                    "SDelete Gui",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }

            return RunTargets(sdeleteExePath, request, targets, options);
        }

        private static List<string> CollectBatchTargets(RunnerRequest request, bool enableBatchCollection)
        {
            if (!enableBatchCollection)
            {
                return new List<string> { request.TargetPath };
            }

            string batchKey = $"{SDeleteCommandBuilder.ToCliToken(request.Mode)}_{request.Passes}";
            string mutexName = $@"Local\SDeleteGui_Batch_{batchKey}";
            string pipeName = $"SDeleteGui_Pipe_{batchKey}";

            bool createdNew;
            using (var mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    if (TryForwardTargetToCollector(pipeName, request.TargetPath))
                    {
                        return null;
                    }

                    // Fall back to direct execution if the primary collector already closed its pipe.
                    return new List<string> { request.TargetPath };
                }

                var targets = new List<string> { request.TargetPath };
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { request.TargetPath };
                ReceiveAdditionalTargets(pipeName, targets, seen);
                return targets;
            }
        }

        private static bool TryForwardTargetToCollector(string pipeName, string targetPath)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                {
                    client.Connect(1500);
                    byte[] payload = Encoding.UTF8.GetBytes(targetPath + "\n");
                    client.Write(payload, 0, payload.Length);
                    client.Flush();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ReceiveAdditionalTargets(
            string pipeName,
            List<string> targets,
            HashSet<string> seen)
        {
            DateTime totalDeadline = DateTime.UtcNow.AddMilliseconds(MaxBatchWaitMilliseconds);

            while (DateTime.UtcNow < totalDeadline)
            {
                int remainingTotalMs = (int)(totalDeadline - DateTime.UtcNow).TotalMilliseconds;
                int waitMs = Math.Min(BatchDebounceMilliseconds, Math.Max(1, remainingTotalMs));

                using (var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous))
                {
                    using (var cts = new CancellationTokenSource(waitMs))
                    {
                        try
                        {
                            Task connectTask = server.WaitForConnectionAsync(cts.Token);
                            connectTask.GetAwaiter().GetResult();

                            using (var reader = new StreamReader(server, Encoding.UTF8))
                            {
                                string line = reader.ReadLine();
                                if (!string.IsNullOrWhiteSpace(line) && seen.Add(line))
                                {
                                    targets.Add(line);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // No more Explorer items arrived within the debounce window.
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
        }

        private static bool ConfirmExecution(RunnerRequest request, List<string> targets)
        {
            var sb = new StringBuilder();
            if (request.Mode == SDeleteMode.Delete)
            {
                sb.AppendLine(string.Format(Messages.ConfirmDeletePrompt, targets.Count, request.Passes));
            }
            else
            {
                string modeLabel = request.Mode == SDeleteMode.ZeroFreeSpace
                    ? Messages.ZeroFreeSpaceShort
                    : Messages.CleanFreeSpaceShort;
                sb.AppendLine(string.Format(Messages.ConfirmFreeSpacePrompt, modeLabel, targets[0], request.Passes));
            }

            sb.AppendLine();
            foreach (string path in targets.Take(MaxTargetsShownInPrompt))
            {
                sb.AppendLine(" - " + path);
            }

            if (targets.Count > MaxTargetsShownInPrompt)
            {
                sb.AppendLine(string.Format(Messages.ConfirmMoreItems, targets.Count - MaxTargetsShownInPrompt));
            }

            sb.AppendLine();
            sb.AppendLine(Messages.ConfirmIrreversibleWarning);

            MessageBoxResult answer = MessageBox.Show(
                sb.ToString(),
                Messages.ConfirmDialogTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            return answer == MessageBoxResult.Yes;
        }

        private static int RunTargets(
            string sdeleteExePath,
            RunnerRequest request,
            List<string> targets,
            SDeleteOptions options)
        {
            int worstExitCode = 0;

            foreach (string target in targets)
            {
                int exitCode;
                string errorDetail = null;

                try
                {
                    string args = SDeleteCommandBuilder.BuildSDeleteArguments(request.Mode, request.Passes, target);
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = sdeleteExePath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = options.SequentialExecution,
                        RedirectStandardError = options.SequentialExecution
                    };

                    using (Process proc = Process.Start(startInfo))
                    {
                        if (proc == null)
                        {
                            exitCode = -1;
                            errorDetail = "Failed to start sdelete.exe.";
                        }
                        else if (options.SequentialExecution)
                        {
                            string stderr = proc.StandardError.ReadToEnd();
                            proc.WaitForExit();
                            exitCode = proc.ExitCode;
                            if (exitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
                            {
                                errorDetail = stderr;
                            }
                        }
                        else
                        {
                            exitCode = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    exitCode = -1;
                    errorDetail = ex.Message;
                }

                if (exitCode != 0)
                {
                    worstExitCode = exitCode;
                }

                if (options.EnableAuditLog)
                {
                    AuditLogger.AppendEntry(options.LogFilePath, request.Mode, request.Passes, target, exitCode, errorDetail);
                }
            }

            return worstExitCode;
        }
    }
}
