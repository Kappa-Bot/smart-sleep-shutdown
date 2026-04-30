using System.Windows;

namespace SmartSleepShutdown.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceCoordinator? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstance = SingleInstanceCoordinator.CreateDefault();
        if (StartupIntent.IsExitRequest(e.Args))
        {
            if (!_singleInstance.IsPrimaryInstance)
            {
                _singleInstance.SignalPrimaryExit();
            }

            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        if (!_singleInstance.IsPrimaryInstance)
        {
            if (StartupIntent.ShouldSignalScheduledCheck(e.Args))
            {
                _singleInstance.SignalPrimaryScheduledCheck();
            }
            else if (StartupIntent.ShouldActivateExistingPrimary(e.Args))
            {
                _singleInstance.SignalPrimaryInstance();
            }

            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        _singleInstance.StartActivationListener(() => Dispatcher.Invoke(mainWindow.ShowFromUserRequest));
        _singleInstance.StartExitListener(() => Dispatcher.Invoke(() =>
        {
            mainWindow.AllowExit();
            Shutdown();
        }));
        _singleInstance.StartScheduledCheckListener(() => Dispatcher.Invoke(mainWindow.RunScheduledCheck));

        if (StartupIntent.ShouldShowMainWindow(e.Args))
        {
            mainWindow.Show();
        }
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        if (MainWindow is MainWindow mainWindow)
        {
            mainWindow.AllowExit();
        }

        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
