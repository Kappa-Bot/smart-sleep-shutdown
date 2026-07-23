using System.Windows;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace Hushward.App.Warnings;

public static class WarningPlacementService
{
    private const double Margin = 16;

    public static System.Windows.Point Calculate(
        System.Windows.Rect workArea,
        System.Windows.Size windowSize,
        double dpiScale)
    {
        var scale = dpiScale <= 0 ? 1 : dpiScale;
        var left = workArea.Left / scale;
        var top = workArea.Top / scale;
        var right = workArea.Right / scale;
        var bottom = workArea.Bottom / scale;
        var x = Math.Max(left, right - windowSize.Width - Margin);
        var y = Math.Max(top, bottom - windowSize.Height - Margin);
        return new System.Windows.Point(x, y);
    }

    public static System.Windows.Point ForActiveMonitor(Window window)
    {
        var screen = Forms.Screen.FromPoint(Forms.Cursor.Position);
        var area = screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(window);
        return Calculate(
            new System.Windows.Rect(area.Left, area.Top, area.Width, area.Height),
            new System.Windows.Size(
                window.ActualWidth > 0 ? window.ActualWidth : window.Width,
                window.ActualHeight > 0 ? window.ActualHeight : window.Height),
            dpi.DpiScaleX);
    }
}
