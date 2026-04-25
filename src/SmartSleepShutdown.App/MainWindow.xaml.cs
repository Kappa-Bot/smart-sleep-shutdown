using System.Windows;
using System.Windows.Input;
using System.Media;
using System.ComponentModel;
using SmartSleepShutdown.App.Settings;
using SmartSleepShutdown.App.ViewModels;
using SmartSleepShutdown.Infrastructure.Power;
using SmartSleepShutdown.Infrastructure.System;

namespace SmartSleepShutdown.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TrayIconService _trayIcon;
    private bool _exitRequested;

    public MainWindow()
    {
        InitializeComponent();

        var clock = new SystemClock();
        _viewModel = new MainWindowViewModel(
            new Win32IdleDetector(clock),
            AggregateContextDetector.CreateDefault(),
            new WindowsShutdownExecutor(),
            clock,
            action => Dispatcher.Invoke(action),
            JsonUserSettingsStore.CreateDefault());

        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _trayIcon = new TrayIconService(_viewModel, ShowMainWindow, ExitApplication);
    }

    protected override void OnPreviewMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        _viewModel.CancelCountdownFromInput();
        base.OnPreviewMouseMove(e);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        _viewModel.CancelCountdownFromInput();
        base.OnPreviewKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _trayIcon.Dispose();
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowStillRunningHint();
            return;
        }

        base.OnClosing(e);
    }

    public void ShowFromUserRequest()
    {
        ShowMainWindow();
    }

    public void AllowExit()
    {
        _exitRequested = true;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsCountdownActive) && _viewModel.IsCountdownActive)
        {
            ShowWarningNotification();
        }
    }

    private void ShowWarningNotification()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
        SystemSounds.Exclamation.Play();
    }

    private void ShowMainWindow()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }
}
