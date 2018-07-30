using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Input;

namespace SDelete_Gui.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; RaisePropertyChanged("ErrorMessage"); }
        }

        private int _numberOfPasses;
        public int NumberOfPasses
        {
            get { return _numberOfPasses; }
            set { _numberOfPasses = value; RaisePropertyChanged("NumberOfPasses"); }
        }

        public ICommand ConfigureCommand { get; set; }
        public ICommand UnConfigureCommand { get; set; }
        public ICommand InfoCommand { get; set; }

        private string _systempath = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        private string _sdeletePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "sdelete.exe");


        public MainViewModel()
        {
            NumberOfPasses = 10;
            ErrorMessage = "Ready to start";
            ConfigureCommand = new RelayCommand(() => ExecuteConfigure());
            UnConfigureCommand = new RelayCommand(() => ExecuteUnConfigure());
            InfoCommand = new RelayCommand(() =>
            {
                System.Diagnostics.Process.Start("https://github.com/Tulpep/SDelete-Gui");
            });
        }

        private void ExecuteConfigure()
        {
            if (!File.Exists(_sdeletePath) && !DownloadSdelete(_sdeletePath))
            {
                ErrorMessage = string.Format("SDelete.exe not found in {0} and cannot be downloaded from the Internet", _systempath);
                return;
            }



            if (AddToAllContextMenus(_sdeletePath, NumberOfPasses))
            {
                ErrorMessage = string.Format("Configured. SDelete will use {0} passes", NumberOfPasses);
            }
            else
            {
                ErrorMessage = "Cannot configure. Check your permissions";
            };
        }
        private bool DownloadSdelete(string downloadPath)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadFile("https://live.sysinternals.com/sdelete.exe", downloadPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        private void ExecuteUnConfigure()
        {
            if (RemoveFromAllContextMenu() && RemoveSdeleteFile())
            {
                ErrorMessage = "Unconfigured";
            }
            else
            {
                ErrorMessage = "Failed unconfiguring";
            };
        }

        private bool AddToAllContextMenus(string sdeletePath, int numberOfPassess)
        {
            foreach (RegistryKey registryKey in GetRegistryKeys(sdeletePath, numberOfPassess))
            {
                bool result = AddToContextMenu(registryKey);
                if (!result) return false;
            }
            return true;
        }

        private bool AddToContextMenu(RegistryKey registryKey)
        {
            Microsoft.Win32.RegistryKey regmenu = null;
            Microsoft.Win32.RegistryKey regcmd = null;

            bool result;
            try
            {
                string keyName = registryKey.RegistryPath + registryKey.ShellName;
                regmenu = Registry.ClassesRoot.CreateSubKey(keyName);
                regcmd = Registry.ClassesRoot.CreateSubKey(keyName + "\\command");
                regcmd.SetValue("", registryKey.Command);
                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                if (regmenu != null)
                    regmenu.Dispose();
                if (regcmd != null)
                    regcmd.Dispose();
            }

            return result;
        }

        private bool RemoveFromAllContextMenu()
        {
            foreach (RegistryKey registryKey in GetRegistryKeys(null, 0))
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(registryKey.RegistryPath + registryKey.ShellName);
                }
                catch
                {
                    return false;
                }
            }
            return true;
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

        private IEnumerable<RegistryKey> GetRegistryKeys(string sdeletePath, int numberOfPassess)
        {
            return new List<RegistryKey>()
            {
                new RegistryKey { RegistryPath = "*\\shell\\",
                                  ShellName = "Secure Delete",
                                  Command = String.Format("{0} -p {1} -s -q \"%1\"", sdeletePath, numberOfPassess) },
                new RegistryKey { RegistryPath = "Directory\\shell\\",
                                  ShellName = "Secure Delete",
                                  Command = String.Format("{0} -p {1} -s -q \"%1\"", sdeletePath, numberOfPassess) },
                 new RegistryKey { RegistryPath = "Drive\\shell\\",
                                  ShellName = "Secure Delete",
                                  Command = String.Format(@"{0} -p {1} -q -z -c %1", sdeletePath, numberOfPassess) }
            };
        }

    }
}
