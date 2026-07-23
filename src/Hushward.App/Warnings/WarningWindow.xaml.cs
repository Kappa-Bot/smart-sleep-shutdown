using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Hushward.App.Accessibility;
using Hushward.App.Localization;
using Hushward.App.ViewModels.Warnings;
using Microsoft.Win32;

namespace Hushward.App.Warnings;

public partial class WarningWindow : Window
{
    private readonly WarningViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private readonly LiveRegionAnnouncer _announcer;

    public WarningWindow(WarningViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _announcer = new LiveRegionAnnouncer(message => LiveAnnouncement.Text = message);
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTick, Dispatcher);
        Loaded += OnLoaded;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public void ShowWarning()
    {
        PositionOnActiveMonitor();
        Show();
        _timer.Start();
    }

    public void HideWarning()
    {
        _timer.Stop();
        _announcer.Reset();
        Hide();
    }

    protected override async void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        await _viewModel.HandleUserInputAsync();
        e.Handled = e.Key == Key.Escape;
        base.OnPreviewKeyDown(e);
    }

    protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        await _viewModel.HandleUserInputAsync();
        base.OnPreviewMouseDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnClosed(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => PositionOnActiveMonitor();

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.Invoke(PositionOnActiveMonitor);

    private void OnTick(object? sender, EventArgs e)
    {
        _viewModel.Tick();
        _announcer.Update(
            _viewModel.RemainingSeconds,
            seconds => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                UiText.WarningSecondsFormat,
                seconds));
    }

    private void PositionOnActiveMonitor()
    {
        var position = WarningPlacementService.ForActiveMonitor(this);
        Left = position.X;
        Top = position.Y;
    }
}
