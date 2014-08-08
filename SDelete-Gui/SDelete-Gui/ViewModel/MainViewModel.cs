using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using System;
using Microsoft.Win32;

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

        private const string _menuEntryTitle = "Secure Delete";

        public MainViewModel()
        {
            ErrorMessage = "Ready to start";
            ConfigureCommand = new RelayCommand(() => ExecuteConfigure());
            UnConfigureCommand = new RelayCommand(() => ExecuteUnConfigure());
        }

        private void ExecuteConfigure()
        {
            const string sDeleteCommand = "sdelete -p 10 -s -q \"%1\"";
            if (AddContextMenuToFiles(FolderOrFile.File, _menuEntryTitle, sDeleteCommand)
                && AddContextMenuToFiles(FolderOrFile.Folder, _menuEntryTitle, sDeleteCommand))
            {
                ErrorMessage = "Configured";
            }
            else
            {
                ErrorMessage = "Removed";
            };
        }
        private void ExecuteUnConfigure()
        {
            if (RemoveContextMenuOfFiles(FolderOrFile.File, _menuEntryTitle)
                && RemoveContextMenuOfFiles(FolderOrFile.Folder, _menuEntryTitle))
            {
                ErrorMessage = "Unconfigured";
            }
            else
            {
                ErrorMessage = "Failed unconfiguring";
            };
        }


        private bool AddContextMenuToFiles(FolderOrFile folderOrFile, string name, string command)
        {
            RegistryKey regmenu = null;
            RegistryKey regcmd = null;
            string keyName;
            switch (folderOrFile)
            {
                case FolderOrFile.File:
                    keyName = "*\\shell\\" + name;
                    break;
                case FolderOrFile.Folder:
                    keyName = "Folder\\shell\\" + name;
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
        private bool RemoveContextMenuOfFiles(FolderOrFile folderOrFile, string name)
        {
            try
            {
                string keyName;
                switch (folderOrFile)
                {
                    case FolderOrFile.File:
                        keyName = "*\\shell\\" + name;
                        break;
                    case FolderOrFile.Folder:
                        keyName = "Folder\\shell\\" + name;
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