using System.ComponentModel;
using Hushward.App.Localization;
using Hushward.App.ViewModels.Tray;
using Hushward.Application.Runtime;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Hushward.App.Tray;

public sealed class TrayIconHost : IDisposable
{
    private readonly TrayFlyoutViewModel _viewModel;
    private readonly TrayFlyoutWindow _flyout;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu = new();
    private Drawing.Icon? _icon;

    public TrayIconHost(TrayFlyoutViewModel viewModel)
    {
        _viewModel = viewModel;
        _flyout = new TrayFlyoutWindow(viewModel);
        _icon = TrayIconFactory.Create(ResolveState());
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = ShortTooltip(viewModel.StateLabel),
            Visible = true,
            ContextMenuStrip = _menu
        };
        _notifyIcon.MouseClick += OnMouseClick;
        _viewModel.PropertyChanged += OnPropertyChanged;
        RebuildMenu();
    }

    public void ShowStillRunningHint()
    {
        _notifyIcon.BalloonTipTitle = UiText.TrayStillRunningTitle;
        _notifyIcon.BalloonTipText = UiText.TrayStillRunningMessage;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnPropertyChanged;
        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _flyout.Close();
        _menu.Dispose();
        _icon?.Dispose();
        _viewModel.Dispose();
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            _flyout.ShowNearNotificationArea();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(TrayFlyoutViewModel.StateLabel)
            or nameof(TrayFlyoutViewModel.Snapshot)))
        {
            return;
        }

        var next = TrayIconFactory.Create(ResolveState());
        var old = _icon;
        _icon = next;
        _notifyIcon.Icon = next;
        _notifyIcon.Text = ShortTooltip(_viewModel.StateLabel);
        old?.Dispose();
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add(new Forms.ToolStripMenuItem(_viewModel.StateLabel) { Enabled = false });
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(new Forms.ToolStripMenuItem(UiText.TrayOpen, null, (_, _) => _viewModel.OpenMainCommand.Execute(null)));
        _menu.Items.Add(new Forms.ToolStripMenuItem(UiText.PauseToday, null, (_, _) => _viewModel.PauseTodayCommand.Execute(null)));
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(new Forms.ToolStripMenuItem(UiText.TrayExit, null, (_, _) => _viewModel.ExitCommand.Execute(null)));
    }

    private TrayVisualState ResolveState() => _viewModel.Snapshot.MonitoringState switch
    {
        RuntimeState.Disabled => TrayVisualState.Off,
        RuntimeState.WaitingForWindow => TrayVisualState.Waiting,
        RuntimeState.Protected => TrayVisualState.Protected,
        RuntimeState.Warning => TrayVisualState.Warning,
        RuntimeState.SafeMode => TrayVisualState.Degraded,
        _ => TrayVisualState.Ready
    };

    private static string ShortTooltip(string value) => value.Length <= 63 ? value : value[..63];
}
