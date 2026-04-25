using System.Runtime.InteropServices;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class CpuUsageContextProbe : IContextProbe
{
    private const double BlockingThreshold = 0.35;
    private const int RequiredConsecutiveSamples = 2;

    private CpuSample? _lastSample;
    private int _highSamples;

    public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            throw new InvalidOperationException("GetSystemTimes failed.");
        }

        var current = new CpuSample(
            ToUInt64(idleTime),
            ToUInt64(kernelTime),
            ToUInt64(userTime));

        if (_lastSample is null)
        {
            _lastSample = current;
            return ValueTask.FromResult<BlockingContext?>(null);
        }

        var previous = _lastSample.Value;
        _lastSample = current;

        var idleDelta = current.Idle - previous.Idle;
        var kernelDelta = current.Kernel - previous.Kernel;
        var userDelta = current.User - previous.User;
        var totalDelta = kernelDelta + userDelta;

        if (totalDelta == 0)
        {
            return ValueTask.FromResult<BlockingContext?>(null);
        }

        var usage = 1d - (double)idleDelta / totalDelta;
        _highSamples = usage >= BlockingThreshold ? _highSamples + 1 : 0;

        var context = _highSamples >= RequiredConsecutiveSamples
            ? new BlockingContext(BlockingContextType.HighCpu, $"CPU usage is {usage:P0}")
            : null;

        return ValueTask.FromResult<BlockingContext?>(context);
    }

    private static ulong ToUInt64(NativeMethods.FileTime fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }

    private readonly record struct CpuSample(ulong Idle, ulong Kernel, ulong User);
}
