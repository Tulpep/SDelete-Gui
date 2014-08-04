using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using System;

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
            throw new NotImplementedException();
        }
    }
}