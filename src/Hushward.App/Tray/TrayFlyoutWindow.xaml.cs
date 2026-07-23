using System.Windows;
using System.Windows.Media;
using Hushward.App.ViewModels.Tray;
using Forms = System.Windows.Forms;

namespace Hushward.App.Tray;

public partial class TrayFlyoutWindow : Window
{
    public TrayFlyoutWindow(TrayFlyoutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Deactivated += (_, _) => Hide();
    }

    public void ShowNearNotificationArea()
    {
        Show();
        var screen = Forms.Screen.FromPoint(Forms.Cursor.Position);
        var area = screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var workArea = new Rect(area.Left, area.Top, area.Width, area.Height);
        var position = Hushward.App.Warnings.WarningPlacementService.Calculate(
            workArea,
            new System.Windows.Size(Width, Height),
            dpi);
        Left = position.X;
        Top = position.Y;
        Activate();
    }
}
