using System.ComponentModel;
using System.Media;
using System.Windows;
using Hushward.App.ViewModels;
using Hushward.Core.Routines;

namespace Hushward.App.Views;

public partial class ShellWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private readonly Func<IDisposable> _sleepBlockerFactory;
    private readonly TrayIconService _trayIcon;
    private IDisposable? _sleepBlocker;
    private bool _exitRequested;

    public ShellWindow(
        ShellViewModel viewModel,
        Func<IDisposable> sleepBlockerFactory)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(sleepBlockerFactory);

        InitializeComponent();
        _viewModel = viewModel;
        _sleepBlockerFactory = sleepBlockerFactory;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _trayIcon = new TrayIconService(_viewModel, ShowMainWindow, ExitApplication);
    }

    public void ShowFromUserRequest() => ShowMainWindow();

    public void RunScheduledCheck() => _viewModel.RunScheduledCheck();

    public void AllowExit() => _exitRequested = true;

    public Task ApplyRoutineAsync(NightRoutine routine) => _viewModel.ApplyRoutineAsync(routine);

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

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        StopPreventingSleep();
        _trayIcon.Dispose();
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.IsCountdownActive))
        {
            return;
        }

        if (_viewModel.IsCountdownActive)
        {
            StartPreventingSleep();
            ShowWarningNotification();
        }
        else
        {
            StopPreventingSleep();
        }
    }

    private void ShowWarningNotification()
    {
        ShowMainWindow();
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

    private void StartPreventingSleep()
    {
        _sleepBlocker ??= _sleepBlockerFactory();
    }

    private void StopPreventingSleep()
    {
        _sleepBlocker?.Dispose();
        _sleepBlocker = null;
    }
}
