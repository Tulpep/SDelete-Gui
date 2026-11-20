using System;
using System.IO;
using Microsoft.Win32;

namespace SDelete_Gui.Services
{
    /// <summary>
    /// Defines the operation mode for SDelete execution.
    /// </summary>
    public enum SDeleteMode
    {
        Delete,
        CleanFreeSpace,
        ZeroFreeSpace
    }

    /// <summary>
    /// Stores validated configuration options for SDelete-Gui.
    /// </summary>
    public sealed class SDeleteOptions
    {
        public const int MinPasses = 1;
        public const int MaxPasses = 100;
        public const int DefaultPassCount = 10;

        public static readonly int[] SubmenuPassPresets = { 1, 3, 5, 7, 35 };

        private const string SettingsRegistryPath = @"Software\SDelete-Gui";

        public int NumberOfPasses { get; set; } = DefaultPassCount;
        public bool ConfirmBeforeDelete { get; set; } = true;
        public bool SequentialExecution { get; set; } = true;
        public bool UsePassSubmenu { get; set; } = true;
        public bool ShowContextMenuIcon { get; set; } = true;
        public bool EnableAuditLog { get; set; } = false;
        public string LogFilePath { get; set; } = GetDefaultLogFilePath();

        public static string GetDefaultLogFilePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "SDelete-Gui", "sdelete-audit.log");
        }

        /// <summary>
        /// Clamps the pass count to the valid range and restores a default log path if empty.
        /// </summary>
        public void Normalize()
        {
            if (NumberOfPasses < MinPasses)
            {
                NumberOfPasses = MinPasses;
            }
            else if (NumberOfPasses > MaxPasses)
            {
                NumberOfPasses = MaxPasses;
            }

            if (string.IsNullOrWhiteSpace(LogFilePath))
            {
                LogFilePath = GetDefaultLogFilePath();
            }
        }

        /// <summary>
        /// Loads saved user options from the current-user registry hive.
        /// </summary>
        public static SDeleteOptions Load()
        {
            var options = new SDeleteOptions();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsRegistryPath))
                {
                    if (key == null)
                    {
                        return options;
                    }

                    options.NumberOfPasses = ReadInt(key, nameof(NumberOfPasses), DefaultPassCount);
                    options.ConfirmBeforeDelete = ReadBool(key, nameof(ConfirmBeforeDelete), true);
                    options.SequentialExecution = ReadBool(key, nameof(SequentialExecution), true);
                    options.UsePassSubmenu = ReadBool(key, nameof(UsePassSubmenu), true);
                    options.ShowContextMenuIcon = ReadBool(key, nameof(ShowContextMenuIcon), true);
                    options.EnableAuditLog = ReadBool(key, nameof(EnableAuditLog), false);
                    options.LogFilePath = key.GetValue(nameof(LogFilePath)) as string ?? GetDefaultLogFilePath();
                }
            }
            catch
            {
                // Return defaults if the registry key is unreadable.
            }

            options.Normalize();
            return options;
        }

        /// <summary>
        /// Saves user options to the current-user registry hive.
        /// </summary>
        public void Save()
        {
            Normalize();
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsRegistryPath))
            {
                if (key == null)
                {
                    return;
                }

                key.SetValue(nameof(NumberOfPasses), NumberOfPasses, RegistryValueKind.DWord);
                key.SetValue(nameof(ConfirmBeforeDelete), ConfirmBeforeDelete ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(nameof(SequentialExecution), SequentialExecution ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(nameof(UsePassSubmenu), UsePassSubmenu ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(nameof(ShowContextMenuIcon), ShowContextMenuIcon ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(nameof(EnableAuditLog), EnableAuditLog ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(nameof(LogFilePath), LogFilePath, RegistryValueKind.String);
            }
        }

        private static int ReadInt(RegistryKey key, string name, int fallback)
        {
            object raw = key.GetValue(name);
            if (raw is int intValue)
            {
                return intValue;
            }

            return int.TryParse(raw?.ToString(), out int parsed) ? parsed : fallback;
        }

        private static bool ReadBool(RegistryKey key, string name, bool fallback)
        {
            object raw = key.GetValue(name);
            if (raw is int intValue)
            {
                return intValue != 0;
            }

            if (raw is string text && bool.TryParse(text, out bool parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
