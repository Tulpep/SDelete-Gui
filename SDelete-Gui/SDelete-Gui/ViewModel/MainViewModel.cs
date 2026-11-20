using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SDelete_Gui.Properties;
using SDelete_Gui.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Input;

namespace SDelete_Gui.ViewModel
{
    /// <summary>
    /// Controls configuration state and commands for the SDelete-Gui main window.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly SDeleteOptions _options;
        private readonly string _systempath = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        private readonly string _sdeletePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
            "sdelete.exe");

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; RaisePropertyChanged(nameof(ErrorMessage)); }
        }

        public int NumberOfPasses
        {
            get { return _options.NumberOfPasses; }
            set
            {
                _options.NumberOfPasses = value;
                _options.Normalize();
                RaisePropertyChanged(nameof(NumberOfPasses));
            }
        }

        public bool ConfirmBeforeDelete
        {
            get { return _options.ConfirmBeforeDelete; }
            set { _options.ConfirmBeforeDelete = value; RaisePropertyChanged(nameof(ConfirmBeforeDelete)); }
        }

        public bool SequentialExecution
        {
            get { return _options.SequentialExecution; }
            set { _options.SequentialExecution = value; RaisePropertyChanged(nameof(SequentialExecution)); }
        }

        public bool UsePassSubmenu
        {
            get { return _options.UsePassSubmenu; }
            set { _options.UsePassSubmenu = value; RaisePropertyChanged(nameof(UsePassSubmenu)); }
        }

        public bool ShowContextMenuIcon
        {
            get { return _options.ShowContextMenuIcon; }
            set { _options.ShowContextMenuIcon = value; RaisePropertyChanged(nameof(ShowContextMenuIcon)); }
        }

        public bool EnableAuditLog
        {
            get { return _options.EnableAuditLog; }
            set { _options.EnableAuditLog = value; RaisePropertyChanged(nameof(EnableAuditLog)); }
        }

        public string LogFilePath
        {
            get { return _options.LogFilePath; }
            set { _options.LogFilePath = value; RaisePropertyChanged(nameof(LogFilePath)); }
        }

        public ICommand ConfigureCommand { get; set; }
        public ICommand UnConfigureCommand { get; set; }
        public ICommand InfoCommand { get; set; }

        public MainViewModel()
        {
            _options = SDeleteOptions.Load();
            ErrorMessage = Messages.ReadyToStart;
            ConfigureCommand = new RelayCommand(ExecuteConfigure);
            UnConfigureCommand = new RelayCommand(ExecuteUnConfigure);
            InfoCommand = new RelayCommand(() =>
            {
                _ = Process.Start("https://github.com/Tulpep/SDelete-Gui#sdelete-gui-");
            });
        }

        private void ExecuteConfigure()
        {
            if (!File.Exists(_sdeletePath) && !DownloadSdelete(_sdeletePath))
            {
                ErrorMessage = string.Format(Messages.SDeleteNotFoundAndNotDownloadable, _systempath);
                return;
            }

            try
            {
                _options.Save();
            }
            catch
            {
                ErrorMessage = Messages.CheckPermissions;
                return;
            }

            string runnerPath = ResolveRunnerExecutablePath();
            if (ContextMenuManager.ApplyAll(runnerPath, _options))
            {
                ErrorMessage = string.Format(Messages.ConfiguredXPasses, NumberOfPasses);
            }
            else
            {
                ErrorMessage = Messages.CheckPermissions;
            }
        }

        private void ExecuteUnConfigure()
        {
            bool menusRemoved = ContextMenuManager.RemoveAll();
            bool fileRemoved = RemoveSdeleteFile();

            if (menusRemoved && fileRemoved)
            {
                ErrorMessage = Messages.Unconfigured;
            }
            else
            {
                ErrorMessage = Messages.FailedUnconfiguring;
            }
        }

        private static string ResolveRunnerExecutablePath()
        {
            try
            {
                string processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(processPath))
                {
                    return processPath;
                }
            }
            catch
            {
                // Fall back to the assembly location if process inspection fails.
            }

            return Assembly.GetExecutingAssembly().Location;
        }

        private static bool DownloadSdelete(string downloadPath)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var client = new WebClient())
            {
                try
                {
                    string url = Environment.Is64BitOperatingSystem
                        ? "https://live.sysinternals.com/sdelete64.exe"
                        : "https://live.sysinternals.com/sdelete.exe";
                    client.DownloadFile(url, downloadPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private bool RemoveSdeleteFile()
        {
            try
            {
                if (File.Exists(_sdeletePath))
                {
                    File.Delete(_sdeletePath);
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
