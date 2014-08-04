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

        public ICommand InstallCommand { get; set; }


        public MainViewModel()
        {
            InstallCommand = new RelayCommand(() => ExecuteInstall());

            ErrorMessage = "Hello";
        }

        private void ExecuteInstall()
        {
            AddContextMenuToFiles("prueba", "notepad.exe %1");
        }


        private bool AddContextMenuToFiles(string name, string command)
        {
            RegistryKey regmenu = null;
            RegistryKey regcmd = null;
            bool result;
            try
            {
                const string menuName = "*\\shell\\";
                string keyName = menuName + name;
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
    }
}