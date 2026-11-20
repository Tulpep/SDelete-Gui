using System;
using System.Collections.Generic;
using Microsoft.Win32;
using SDelete_Gui.Properties;

namespace SDelete_Gui.Services
{
    /// <summary>
    /// Represents a single Explorer context-menu entry or cascading submenu.
    /// </summary>
    public sealed class ContextMenuSpec
    {
        public string RegistryPath { get; set; }
        public string KeyName { get; set; }
        public string MuiVerb { get; set; }
        public string Icon { get; set; }
        public string Command { get; set; }
        public List<ContextMenuSpec> SubItems { get; } = new List<ContextMenuSpec>();
    }

    /// <summary>
    /// Builds and applies Windows Explorer context-menu registry keys for SDelete-Gui.
    /// </summary>
    public static class ContextMenuManager
    {
        public const string RootShellKeyName = "SDeleteGui";

        // Yellow warning triangle icon from shell32.dll for clear destructive-action visibility.
        public const string DefaultWarningIcon = "imageres.dll,-84";

        private static readonly string[] TargetRoots =
        {
            @"*\shell\",
            @"Directory\shell\",
            @"Drive\shell\"
        };

        private static readonly string[] LegacyKeyNames =
        {
            "Secure Delete",
            "Borrado Seguro",
            "Sicher Löschen"
        };

        /// <summary>
        /// Builds the complete set of context-menu definitions for files, directories, and drives.
        /// </summary>
        public static IReadOnlyList<ContextMenuSpec> BuildMenuSpecs(string runnerExePath, SDeleteOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Normalize();
            string iconValue = options.ShowContextMenuIcon ? DefaultWarningIcon : null;
            var specs = new List<ContextMenuSpec>();

            specs.Add(BuildDeleteMenuSpec(@"*\shell\", runnerExePath, options, iconValue));
            specs.Add(BuildDeleteMenuSpec(@"Directory\shell\", runnerExePath, options, iconValue));
            specs.Add(BuildDriveMenuSpec(runnerExePath, options, iconValue));

            return specs;
        }

        private static ContextMenuSpec BuildDeleteMenuSpec(
            string rootPath,
            string runnerExePath,
            SDeleteOptions options,
            string iconValue)
        {
            var root = new ContextMenuSpec
            {
                RegistryPath = rootPath,
                KeyName = RootShellKeyName,
                MuiVerb = Messages.SecureDelete,
                Icon = iconValue
            };

            if (!options.UsePassSubmenu)
            {
                root.Command = SDeleteCommandBuilder.BuildShellRunnerCommand(
                    runnerExePath,
                    SDeleteMode.Delete,
                    options.NumberOfPasses);
                return root;
            }

            // Add default configured pass count first, then standard quick presets.
            root.SubItems.Add(new ContextMenuSpec
            {
                KeyName = "00_default",
                MuiVerb = string.Format(Messages.SubmenuDefaultPasses, options.NumberOfPasses),
                Icon = iconValue,
                Command = SDeleteCommandBuilder.BuildShellRunnerCommand(
                    runnerExePath,
                    SDeleteMode.Delete,
                    options.NumberOfPasses)
            });

            int order = 1;
            foreach (int preset in SDeleteOptions.SubmenuPassPresets)
            {
                if (preset == options.NumberOfPasses)
                {
                    continue;
                }

                string label = preset == 1
                    ? Messages.SubmenuOnePass
                    : string.Format(Messages.SubmenuNPasses, preset);

                root.SubItems.Add(new ContextMenuSpec
                {
                    KeyName = $"{order:D2}_passes_{preset}",
                    MuiVerb = label,
                    Icon = iconValue,
                    Command = SDeleteCommandBuilder.BuildShellRunnerCommand(
                        runnerExePath,
                        SDeleteMode.Delete,
                        preset)
                });
                order++;
            }

            return root;
        }

        private static ContextMenuSpec BuildDriveMenuSpec(
            string runnerExePath,
            SDeleteOptions options,
            string iconValue)
        {
            var driveRoot = new ContextMenuSpec
            {
                RegistryPath = @"Drive\shell\",
                KeyName = RootShellKeyName,
                MuiVerb = Messages.EraseFreeSpaceMenu,
                Icon = iconValue
            };

            driveRoot.SubItems.Add(new ContextMenuSpec
            {
                KeyName = "01_clean_free_space",
                MuiVerb = string.Format(Messages.CleanFreeSpaceLabel, options.NumberOfPasses),
                Icon = iconValue,
                Command = SDeleteCommandBuilder.BuildShellRunnerCommand(
                    runnerExePath,
                    SDeleteMode.CleanFreeSpace,
                    options.NumberOfPasses)
            });

            driveRoot.SubItems.Add(new ContextMenuSpec
            {
                KeyName = "02_zero_free_space",
                MuiVerb = string.Format(Messages.ZeroFreeSpaceLabel, options.NumberOfPasses),
                Icon = iconValue,
                Command = SDeleteCommandBuilder.BuildShellRunnerCommand(
                    runnerExePath,
                    SDeleteMode.ZeroFreeSpace,
                    options.NumberOfPasses)
            });

            return driveRoot;
        }

        /// <summary>
        /// Writes all configured context menus under HKEY_CLASSES_ROOT.
        /// </summary>
        public static bool ApplyAll(string runnerExePath, SDeleteOptions options)
        {
            RemoveAll();

            IReadOnlyList<ContextMenuSpec> specs = BuildMenuSpecs(runnerExePath, options);
            foreach (ContextMenuSpec spec in specs)
            {
                if (!WriteSpec(spec))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes both current and legacy SDelete-Gui context-menu entries.
        /// </summary>
        public static bool RemoveAll()
        {
            bool allSucceeded = true;
            var keysToRemove = new List<string>(LegacyKeyNames) { RootShellKeyName };
            if (!keysToRemove.Contains(Messages.SecureDelete))
            {
                keysToRemove.Add(Messages.SecureDelete);
            }

            foreach (string root in TargetRoots)
            {
                foreach (string keyName in keysToRemove)
                {
                    try
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(root + keyName, throwOnMissingSubKey: false);
                    }
                    catch
                    {
                        allSucceeded = false;
                    }
                }
            }

            return allSucceeded;
        }

        private static bool WriteSpec(ContextMenuSpec spec)
        {
            try
            {
                string fullKeyPath = spec.RegistryPath + spec.KeyName;
                using (RegistryKey menuKey = Registry.ClassesRoot.CreateSubKey(fullKeyPath))
                {
                    if (menuKey == null)
                    {
                        return false;
                    }

                    menuKey.SetValue("MUIVerb", spec.MuiVerb, RegistryValueKind.String);
                    if (!string.IsNullOrWhiteSpace(spec.Icon))
                    {
                        menuKey.SetValue("Icon", spec.Icon, RegistryValueKind.String);
                    }

                    if (spec.SubItems.Count > 0)
                    {
                        // Empty SubCommands tells Explorer to load child entries from the inline shell subkey.
                        menuKey.SetValue("SubCommands", string.Empty, RegistryValueKind.String);

                        foreach (ContextMenuSpec child in spec.SubItems)
                        {
                            string childPath = $@"{fullKeyPath}\shell\{child.KeyName}";
                            using (RegistryKey childKey = Registry.ClassesRoot.CreateSubKey(childPath))
                            {
                                if (childKey == null)
                                {
                                    return false;
                                }

                                childKey.SetValue("MUIVerb", child.MuiVerb, RegistryValueKind.String);
                                if (!string.IsNullOrWhiteSpace(child.Icon))
                                {
                                    childKey.SetValue("Icon", child.Icon, RegistryValueKind.String);
                                }

                                using (RegistryKey cmdKey = Registry.ClassesRoot.CreateSubKey(childPath + @"\command"))
                                {
                                    if (cmdKey == null)
                                    {
                                        return false;
                                    }

                                    cmdKey.SetValue(string.Empty, child.Command, RegistryValueKind.String);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (RegistryKey cmdKey = Registry.ClassesRoot.CreateSubKey(fullKeyPath + @"\command"))
                        {
                            if (cmdKey == null)
                            {
                                return false;
                            }

                            cmdKey.SetValue(string.Empty, spec.Command, RegistryValueKind.String);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
