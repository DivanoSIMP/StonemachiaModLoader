using StonemachiaModLoader.Core;
using System.Windows;

namespace StonemachiaModLoader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.CleanLatestLog();

            Logger.Info("Application starting.");
            Logger.Info($"OS: {Environment.OSVersion}");
            Logger.Info($".NET: {Environment.Version}");

            Dispatcher.UnhandledException += (_, args) =>
            {
                Logger.Error("Unhandled UI Exception", args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    Logger.Error("Unhandled application exception.", ex);
                else
                    Logger.Error($"Unhandled exception: {args.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Logger.Error("Unobserved Task Exception.", args.Exception);

                args.SetObserved();
            };

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info($"Application exiting. Exit code: {e.ApplicationExitCode}");
            base.OnExit(e);
        }
    }

}
