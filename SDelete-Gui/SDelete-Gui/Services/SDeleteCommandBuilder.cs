using System;

namespace SDelete_Gui.Services
{
    /// <summary>
    /// Holds parsed command-line arguments for a shell runner invocation.
    /// </summary>
    public sealed class RunnerRequest
    {
        public SDeleteMode Mode { get; set; }
        public int Passes { get; set; }
        public string TargetPath { get; set; }
    }

    /// <summary>
    /// Builds and parses command-line arguments for SDelete-Gui and Sysinternals SDelete.
    /// </summary>
    public static class SDeleteCommandBuilder
    {
        public const string RunFlag = "--run";
        public const string ModeFlag = "--mode";
        public const string PassesFlag = "--passes";
        public const string TargetFlag = "--target";

        /// <summary>
        /// Converts a mode enum to its stable CLI token.
        /// </summary>
        public static string ToCliToken(SDeleteMode mode)
        {
            switch (mode)
            {
                case SDeleteMode.CleanFreeSpace:
                    return "clean-free-space";
                case SDeleteMode.ZeroFreeSpace:
                    return "zero-free-space";
                default:
                    return "delete";
            }
        }

        /// <summary>
        /// Parses a CLI mode token into a mode enum.
        /// </summary>
        public static bool TryParseMode(string token, out SDeleteMode mode)
        {
            if (string.Equals(token, "clean-free-space", StringComparison.OrdinalIgnoreCase))
            {
                mode = SDeleteMode.CleanFreeSpace;
                return true;
            }

            if (string.Equals(token, "zero-free-space", StringComparison.OrdinalIgnoreCase))
            {
                mode = SDeleteMode.ZeroFreeSpace;
                return true;
            }

            if (string.Equals(token, "delete", StringComparison.OrdinalIgnoreCase))
            {
                mode = SDeleteMode.Delete;
                return true;
            }

            mode = SDeleteMode.Delete;
            return false;
        }

        /// <summary>
        /// Builds the Explorer shell command string that invokes SDelete-Gui in runner mode.
        /// </summary>
        public static string BuildShellRunnerCommand(string runnerExePath, SDeleteMode mode, int passes)
        {
            ValidatePasses(passes);
            if (string.IsNullOrWhiteSpace(runnerExePath))
            {
                throw new ArgumentException("Runner executable path must not be empty.", nameof(runnerExePath));
            }

            string modeToken = ToCliToken(mode);
            // Pass %1 without a trailing backslash escape issue by wrapping in quotes cleanly.
            return $"\"{runnerExePath}\" {RunFlag} {ModeFlag} {modeToken} {PassesFlag} {passes} {TargetFlag} \"%1\"";
        }

        /// <summary>
        /// Builds the argument string for Sysinternals sdelete.exe.
        /// </summary>
        public static string BuildSDeleteArguments(SDeleteMode mode, int passes, string targetPath)
        {
            ValidatePasses(passes);
            string normalizedTarget = NormalizeTargetPath(targetPath, mode);
            if (string.IsNullOrWhiteSpace(normalizedTarget))
            {
                throw new ArgumentException("Target path must not be empty.", nameof(targetPath));
            }

            switch (mode)
            {
                case SDeleteMode.CleanFreeSpace:
                    return $"-accepteula -p {passes} -q -c {QuoteForProcess(normalizedTarget)}";
                case SDeleteMode.ZeroFreeSpace:
                    return $"-accepteula -p {passes} -q -z {QuoteForProcess(normalizedTarget)}";
                default:
                    return $"-accepteula -p {passes} -s -q {QuoteForProcess(normalizedTarget)}";
            }
        }

        /// <summary>
        /// Parses startup arguments to determine whether the process runs as a shell runner.
        /// </summary>
        public static bool TryParseRunnerArgs(string[] args, out RunnerRequest request)
        {
            request = null;
            if (args == null || args.Length == 0)
            {
                return false;
            }

            bool hasRunFlag = false;
            SDeleteMode mode = SDeleteMode.Delete;
            int passes = SDeleteOptions.DefaultPassCount;
            string target = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, RunFlag, StringComparison.OrdinalIgnoreCase))
                {
                    hasRunFlag = true;
                }
                else if (string.Equals(arg, ModeFlag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (!TryParseMode(args[++i], out mode))
                    {
                        return false;
                    }
                }
                else if (string.Equals(arg, PassesFlag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (!int.TryParse(args[++i], out passes))
                    {
                        return false;
                    }
                }
                else if (string.Equals(arg, TargetFlag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    target = args[++i];
                }
            }

            if (!hasRunFlag || string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            if (passes < SDeleteOptions.MinPasses || passes > SDeleteOptions.MaxPasses)
            {
                return false;
            }

            request = new RunnerRequest
            {
                Mode = mode,
                Passes = passes,
                TargetPath = NormalizeTargetPath(target, mode)
            };
            return true;
        }

        /// <summary>
        /// Normalizes drive roots and strips stray trailing quotes caused by Explorer backslash escaping.
        /// </summary>
        public static string NormalizeTargetPath(string rawTarget, SDeleteMode mode)
        {
            if (string.IsNullOrWhiteSpace(rawTarget))
            {
                return string.Empty;
            }

            string cleaned = rawTarget.Trim().Trim('"');
            if (mode == SDeleteMode.CleanFreeSpace || mode == SDeleteMode.ZeroFreeSpace)
            {
                // Convert C:\ or C: to the C: form that sdelete expects for volume free space.
                if (cleaned.Length >= 2 && char.IsLetter(cleaned[0]) && cleaned[1] == ':')
                {
                    return cleaned.Substring(0, 2);
                }
            }

            return cleaned;
        }

        private static string QuoteForProcess(string value)
        {
            // Avoid a trailing backslash right before a closing quote in Windows argument parsing.
            if (value.EndsWith("\\", StringComparison.Ordinal) && !value.EndsWith("\\\\", StringComparison.Ordinal))
            {
                value += "\\";
            }

            return $"\"{value}\"";
        }

        private static void ValidatePasses(int passes)
        {
            if (passes < SDeleteOptions.MinPasses || passes > SDeleteOptions.MaxPasses)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(passes),
                    $"Passes must be between {SDeleteOptions.MinPasses} and {SDeleteOptions.MaxPasses}.");
            }
        }
    }
}
