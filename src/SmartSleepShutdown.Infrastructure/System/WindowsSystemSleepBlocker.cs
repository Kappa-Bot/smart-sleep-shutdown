using System.Runtime.InteropServices;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class WindowsSystemSleepBlocker : IDisposable
{
    private const ExecutionState Continuous = ExecutionState.EsContinuous;
    private const ExecutionState SystemRequired = ExecutionState.EsSystemRequired;
    private static readonly ExecutionState ActiveFlags = Continuous | SystemRequired;

    private bool _disposed;

    private WindowsSystemSleepBlocker()
    {
        SetThreadExecutionState(ActiveFlags);
    }

    public static IReadOnlyList<string> ActiveFlagNames { get; } =
    [
        "ES_CONTINUOUS",
        "ES_SYSTEM_REQUIRED"
    ];

    public static WindowsSystemSleepBlocker Start()
    {
        return new WindowsSystemSleepBlocker();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SetThreadExecutionState(Continuous);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [Flags]
    private enum ExecutionState : uint
    {
        EsContinuous = 0x80000000,
        EsSystemRequired = 0x00000001
    }
}
