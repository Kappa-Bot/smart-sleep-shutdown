using System.Runtime.InteropServices;

namespace SmartSleepShutdown.Infrastructure.System;

internal static partial class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [DllImport("kernel32.dll")]
    public static extern ulong GetTickCount64();

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr windowHandle, out Rect rect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfo monitorInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        public uint CbSize;
        public uint DwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfo
    {
        public uint CbSize;
        public Rect RcMonitor;
        public Rect RcWork;
        public uint DwFlags;

        public static MonitorInfo Create()
        {
            return new MonitorInfo { CbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }
}
