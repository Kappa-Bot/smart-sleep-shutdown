using System.ComponentModel;
using System.Runtime.InteropServices;
using SmartSleepShutdown.Core.Abstractions;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class Win32IdleDetector : IIdleDetector
{
    private readonly ISystemClock _clock;
    private TimeSpan? _lastIdleDuration;

    public Win32IdleDetector(ISystemClock clock)
    {
        _clock = clock;
    }

    public ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var info = new NativeMethods.LastInputInfo
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.LastInputInfo>()
        };

        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetLastInputInfo failed.");
        }

        var currentTick = unchecked((uint)NativeMethods.GetTickCount64());
        var idleMilliseconds = unchecked(currentTick - info.DwTime);
        var idleDuration = TimeSpan.FromMilliseconds(idleMilliseconds);
        var inputDetected = _lastIdleDuration.HasValue && idleDuration < _lastIdleDuration.Value;
        _lastIdleDuration = idleDuration;

        return ValueTask.FromResult(new IdleSnapshot(_clock.Now, idleDuration, inputDetected));
    }
}
