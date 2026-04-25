using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SmartSleepShutdown.App;

public static class TrayIconFactory
{
    public static Drawing.Icon Create(TrayVisualState state)
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Drawing.Color.Transparent);

        using var background = new Drawing.SolidBrush(Drawing.Color.FromArgb(18, 30, 48));
        graphics.FillEllipse(background, 1, 1, 30, 30);

        using var moon = new Drawing.SolidBrush(Drawing.Color.FromArgb(248, 250, 252));
        using var cutout = new Drawing.SolidBrush(Drawing.Color.FromArgb(18, 30, 48));
        graphics.FillEllipse(moon, 8, 6, 14, 18);
        graphics.FillEllipse(cutout, 13, 4, 13, 20);

        var indicatorColor = state switch
        {
            TrayVisualState.Active => Drawing.Color.FromArgb(22, 163, 74),
            TrayVisualState.SuspendedToday => Drawing.Color.FromArgb(245, 158, 11),
            _ => Drawing.Color.FromArgb(100, 116, 139)
        };

        using var indicator = new Drawing.SolidBrush(indicatorColor);
        using var border = new Drawing.Pen(Drawing.Color.White, 2);
        graphics.FillEllipse(indicator, 18, 18, 12, 12);
        graphics.DrawEllipse(border, 18, 18, 12, 12);

        if (state == TrayVisualState.SuspendedToday)
        {
            using var pausePen = new Drawing.Pen(Drawing.Color.White, 2);
            graphics.DrawLine(pausePen, 22, 21, 22, 27);
            graphics.DrawLine(pausePen, 26, 21, 26, 27);
        }

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = Drawing.Icon.FromHandle(iconHandle);
            return (Drawing.Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
