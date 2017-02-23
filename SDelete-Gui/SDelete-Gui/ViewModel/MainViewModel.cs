using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
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

        public ICommand ConfigureCommand { get; set; }
        public ICommand UnConfigureCommand { get; set; }
        public ICommand InfoCommand { get; set; }

        private const string MENU_ENTRY_TITLE = "Secure Delete";


        public MainViewModel()
        {
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
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string sdeletePath = Path.Combine(systemFolder, "sdelete.exe");
            if (!DownloadSdelete(sdeletePath))
            {
                ErrorMessage = "Cannot download SDelete right now or it does not exists in " + systemFolder;
                return;
            }

            string sDeleteCommand = String.Format("{0} -p 10 -s -q \"%1\"", sdeletePath);
            if (AddContextMenuToFiles(KeyLocation.File, MENU_ENTRY_TITLE, sDeleteCommand)
                && AddContextMenuToFiles(KeyLocation.Folder, MENU_ENTRY_TITLE, sDeleteCommand)
                && AddContextMenuToFiles(KeyLocation.Drive, MENU_ENTRY_TITLE, sDeleteCommand))
            {
                ErrorMessage = "Configured";
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
                    client.DownloadFile("http://live.sysinternals.com/sdelete.exe", downloadPath);
                    return true;
                }
                catch
                {
                    return File.Exists(downloadPath);
                }
            }
        }
        private void ExecuteUnConfigure()
        {
            if (RemoveContextMenuOfFiles(KeyLocation.File, MENU_ENTRY_TITLE)
                && RemoveContextMenuOfFiles(KeyLocation.Folder, MENU_ENTRY_TITLE))
            {
                ErrorMessage = "Unconfigured";
            }
            else
            {
                ErrorMessage = "Failed unconfiguring";
            };
        }


        private bool AddContextMenuToFiles(KeyLocation folderOrFile, string name, string command)
        {
            RegistryKey regmenu = null;
            RegistryKey regcmd = null;
            string keyName;
            switch (folderOrFile)
            {
                case KeyLocation.File:
                    keyName = "*\\shell\\" + name;
                    break;
                case KeyLocation.Folder:
                    keyName = "Directory\\shell\\" + name;
                    break;
                case KeyLocation.Drive:
                    keyName = "Drive\\shell\\" + name;
                    break;
                default:
                    throw new NotImplementedException();
            }

            bool result;
            try
            {
                regmenu = Registry.ClassesRoot.CreateSubKey(keyName);
                regcmd = Registry.ClassesRoot.CreateSubKey(keyName + "\\command");
                regcmd.SetValue("", command);
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
        private bool RemoveContextMenuOfFiles(KeyLocation folderOrFile, string name)
        {
            try
            {
                string keyName;
                switch (folderOrFile)
                {
                    case KeyLocation.File:
                        keyName = "*\\shell\\" + name;
                        break;
                    case KeyLocation.Folder:
                        keyName = "Directory\\shell\\" + name;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Registry.ClassesRoot.DeleteSubKeyTree(keyName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
