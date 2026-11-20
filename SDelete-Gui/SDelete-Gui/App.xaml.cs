using System;
using System.IO;
using System.Windows;
using SDelete_Gui.Services;

namespace SDelete_Gui
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (SDeleteCommandBuilder.TryParseRunnerArgs(e.Args, out RunnerRequest request))
            {
                string sdeletePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
                    "sdelete.exe");
                int exitCode = BatchRunner.Execute(request, sdeletePath);
                Shutdown(exitCode);
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
    }
}
