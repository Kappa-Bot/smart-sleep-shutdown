using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using SmartSleepShutdown.App.ViewModels;

namespace SmartSleepShutdown.App;

public sealed class TrayIconService : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Action _openWindow;
    private readonly Action _exitApplication;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu = new();
    private Drawing.Icon? _currentIcon;

    public TrayIconService(
        MainWindowViewModel viewModel,
        Action openWindow,
        Action exitApplication)
    {
        _viewModel = viewModel;
        _openWindow = openWindow;
        _exitApplication = exitApplication;

        _currentIcon = TrayIconFactory.Create(TrayVisualStateResolver.Resolve(_viewModel));
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _currentIcon,
            Text = ShortTooltip(_viewModel.TrayStatusText),
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += OnOpenRequested;
        _notifyIcon.MouseClick += OnMouseClick;
        _menu.Opening += OnMenuOpening;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RebuildMenu();
    }

    public void ShowStillRunningHint()
    {
        _notifyIcon.Visible = true;
        _notifyIcon.BalloonTipTitle = TrayMenuText.StillRunningTitle;
        _notifyIcon.BalloonTipText = TrayMenuText.StillRunningMessage;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _menu.Opening -= OnMenuOpening;
        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.DoubleClick -= OnOpenRequested;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
        _menu.Dispose();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.RefreshTemporaryDisableStatus();
        RebuildMenu();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.TrayStatusText)
            or nameof(MainWindowViewModel.IsEnabled)
            or nameof(MainWindowViewModel.IsTemporarilyDisabled))
        {
            _notifyIcon.Text = ShortTooltip(_viewModel.TrayStatusText);
            UpdateIcon();
            RebuildMenu();
        }
    }

    private void OnOpenRequested(object? sender, EventArgs e)
    {
        _openWindow();
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            _openWindow();
        }
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        _menu.Items.Add(new Forms.ToolStripMenuItem(_viewModel.TrayStatusText) { Enabled = false });
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(new Forms.ToolStripMenuItem(TrayMenuText.Open, null, (_, _) => _openWindow()));

        var toggleText = _viewModel.IsTemporarilyDisabled
            ? TrayMenuText.EnableNow
            : _viewModel.IsEnabled ? TrayMenuText.Disable : TrayMenuText.Enable;
        _menu.Items.Add(new Forms.ToolStripMenuItem(toggleText, null, (_, _) => ToggleEnabled()));

        _menu.Items.Add(new Forms.ToolStripMenuItem(
            TrayMenuText.DisableUntilTomorrow,
            null,
            (_, _) => _viewModel.DisableUntilTomorrow())
        {
            Enabled = !_viewModel.IsTemporarilyDisabled
        });

        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(new Forms.ToolStripMenuItem(TrayMenuText.Exit, null, (_, _) => _exitApplication()));
    }

    private void UpdateIcon()
    {
        var nextIcon = TrayIconFactory.Create(TrayVisualStateResolver.Resolve(_viewModel));
        var oldIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        oldIcon?.Dispose();
    }

    private void ToggleEnabled()
    {
        if (_viewModel.IsTemporarilyDisabled)
        {
            _viewModel.ReactivateToday();
        }
        else
        {
            _viewModel.IsEnabled = !_viewModel.IsEnabled;
        }
    }

    private static string ShortTooltip(string text)
    {
        return text.Length <= 63 ? text : text[..63];
    }
}
