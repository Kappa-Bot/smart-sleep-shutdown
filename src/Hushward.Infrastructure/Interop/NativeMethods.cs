using System.Runtime.InteropServices;

namespace Hushward.Infrastructure.Interop;

internal static partial class NativeMethods
{
    public const int SmRemoteSession = 0x1000;
    private const uint DesktopSwitchDesktop = 0x0100;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SystemPowerStatus systemPowerStatus);

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPwrCapabilities(out SystemPowerCapabilities systemPowerCapabilities);

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LockWorkStation();

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint desiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SwitchDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [DllImport("kernel32.dll")]
    public static extern ulong GetTickCount64();

    public static bool IsWorkstationLocked()
    {
        var desktop = OpenInputDesktop(0, false, DesktopSwitchDesktop);
        if (desktop == IntPtr.Zero)
        {
            return true;
        }

        try
        {
            return !SwitchDesktop(desktop);
        }
        finally
        {
            CloseDesktop(desktop);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        public uint CbSize;
        public uint DwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemPowerCapabilities
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool PowerButtonPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool SleepButtonPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool LidPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemS1;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemS2;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemS3;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemS4;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemS5;

        [MarshalAs(UnmanagedType.U1)]
        public bool HiberFilePresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool FullWake;

        [MarshalAs(UnmanagedType.U1)]
        public bool VideoDimPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool ApmPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool UpsPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool ThermalControl;

        [MarshalAs(UnmanagedType.U1)]
        public bool ProcessorThrottle;

        public byte ProcessorMinThrottle;
        public byte ProcessorMaxThrottle;

        [MarshalAs(UnmanagedType.U1)]
        public bool FastSystemS4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Spare2;

        [MarshalAs(UnmanagedType.U1)]
        public bool DiskSpinDown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Spare3;

        [MarshalAs(UnmanagedType.U1)]
        public bool SystemBatteriesPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool BatteriesAreShortTerm;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public BatteryReportingScale[] BatteryScale;

        public int AcOnLineWake;
        public int SoftLidWake;
        public int RtcWake;
        public int MinDeviceWakeState;
        public int DefaultLowLatencyWake;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BatteryReportingScale
    {
        public uint Granularity;
        public uint Capacity;
    }
}
