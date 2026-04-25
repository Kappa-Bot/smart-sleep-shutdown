using System.ComponentModel;
using System.Runtime.InteropServices;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class ForegroundFullscreenContextProbe : IContextProbe
{
    private const uint MonitorDefaultToNearest = 0x00000002;

    public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return ValueTask.FromResult<BlockingContext?>(null);
        }

        var monitor = NativeMethods.MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return ValueTask.FromResult<BlockingContext?>(null);
        }

        if (!NativeMethods.GetWindowRect(foregroundWindow, out var windowRect))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetWindowRect failed.");
        }

        var monitorInfo = NativeMethods.MonitorInfo.Create();
        if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetMonitorInfo failed.");
        }

        var monitorRect = monitorInfo.RcMonitor;
        var fullscreen = windowRect.Left <= monitorRect.Left
            && windowRect.Top <= monitorRect.Top
            && windowRect.Right >= monitorRect.Right
            && windowRect.Bottom >= monitorRect.Bottom;

        var context = fullscreen
            ? new BlockingContext(BlockingContextType.FullScreenApp, "Foreground window is fullscreen")
            : null;

        return ValueTask.FromResult<BlockingContext?>(context);
    }
}
