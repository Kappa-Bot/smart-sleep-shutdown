using System.Windows;
using Hushward.App.Settings;
using Hushward.App.Runtime;
using Hushward.App.ViewModels;
using Hushward.App.Views;
using Hushward.Application.Runtime;
using Hushward.Infrastructure.Power;
using Hushward.Infrastructure.System;

namespace Hushward.App;

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

        var mainWindow = CreateShellWindow();
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
        if (MainWindow is ShellWindow mainWindow)
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

    private ShellWindow CreateShellWindow()
    {
        var clock = new SystemClock();
        var idleDetector = new Win32IdleDetector(clock);
        var contextDetector = AggregateContextDetector.CreateDefault();
        var snapshots = new RuntimeSnapshotPublisher(NightRuntimeSnapshot.Empty(0, clock.Now));
        MainWindowViewModel? viewModel = null;
        var shutdownExecutor = new CoordinatedShutdownExecutor(
            idleDetector,
            contextDetector,
            clock,
            () => viewModel?.CreateSettings() ?? throw new InvalidOperationException("View model is not ready."),
            new WindowsNightActionExecutor(),
            snapshots);
        viewModel = new MainWindowViewModel(
            idleDetector,
            contextDetector,
            shutdownExecutor,
            clock,
            action => Dispatcher.Invoke(action),
            JsonUserSettingsStore.CreateDefault(),
            shutdownExecutor,
            snapshots);

        var shellViewModel = new ShellViewModel(
            viewModel,
            snapshots,
            action => Dispatcher.Invoke(action));
        return new ShellWindow(shellViewModel, WindowsSystemSleepBlocker.Start);
    }
}
