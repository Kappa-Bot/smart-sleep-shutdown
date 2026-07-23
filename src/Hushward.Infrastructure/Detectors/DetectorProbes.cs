using System.Diagnostics;
using System.Net.NetworkInformation;
using Hushward.Core.Protections;
using Hushward.Infrastructure.System;
using Microsoft.Win32;

namespace Hushward.Infrastructure.Detectors;

internal sealed record ActivityEvidence(
    bool IsAvailable,
    bool IsActive,
    string? FriendlyLabel = null);

internal sealed record LoadSample(
    DateTimeOffset ObservedAt,
    double Value);

public sealed record ProtectedProcessRule(
    string ProcessName,
    ProtectionClass ProtectionClass,
    string FriendlyLabel);

internal interface IActivityEvidenceProbe
{
    Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken);
}

internal sealed class DelegateActivityEvidenceProbe : IActivityEvidenceProbe
{
    private readonly Func<ActivityEvidence> _read;

    public DelegateActivityEvidenceProbe(Func<ActivityEvidence> read)
    {
        _read = read;
    }

    public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_read());
    }
}

internal interface ILoadSampleProbe
{
    Task<LoadSample> ReadAsync(CancellationToken cancellationToken);
}

internal sealed class DelegateLoadSampleProbe : ILoadSampleProbe
{
    private readonly Func<LoadSample> _read;

    public DelegateLoadSampleProbe(Func<LoadSample> read)
    {
        _read = read;
    }

    public Task<LoadSample> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_read());
    }
}

internal sealed class ContextProbeActivityEvidenceProbe : IActivityEvidenceProbe
{
    private readonly IContextProbe _probe;
    private readonly string? _friendlyLabel;

    public ContextProbeActivityEvidenceProbe(IContextProbe probe, string? friendlyLabel = null)
    {
        _probe = probe;
        _friendlyLabel = friendlyLabel;
    }

    public async Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken)
    {
        var context = await _probe.DetectAsync(cancellationToken).ConfigureAwait(false);
        return new ActivityEvidence(IsAvailable: true, context is not null, _friendlyLabel);
    }
}

internal sealed class MeetingActivityEvidenceProbe : IActivityEvidenceProbe
{
    private static readonly (string ProcessName, string Label)[] MeetingProcesses =
    [
        ("Teams", "Teams"),
        ("ms-teams", "Teams"),
        ("Zoom", "Zoom")
    ];

    public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var (processName, label) in MeetingProcesses)
        {
            if (Process.GetProcessesByName(processName).Length > 0)
            {
                return Task.FromResult(new ActivityEvidence(IsAvailable: false, IsActive: false, label));
            }
        }

        return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: false));
    }
}

internal sealed class ProtectedProcessActivityProbe : IActivityEvidenceProbe
{
    private readonly IReadOnlyList<ProtectedProcessRule> _rules;

    public ProtectedProcessActivityProbe(IReadOnlyList<ProtectedProcessRule> rules)
    {
        _rules = rules;
    }

    public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var rule in _rules)
        {
            if (Process.GetProcessesByName(rule.ProcessName).Length > 0)
            {
                return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: true, rule.FriendlyLabel));
            }
        }

        return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: false));
    }
}

internal sealed class WindowsUpdateActivityProbe : IActivityEvidenceProbe
{
    private static readonly string[] RebootRequiredKeys =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired",
        @"SYSTEM\CurrentControlSet\Control\Session Manager"
    ];

    public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var keyPath in RebootRequiredKeys)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key is null)
            {
                continue;
            }

            if (keyPath.EndsWith("Session Manager", StringComparison.Ordinal))
            {
                if (key.GetValue("PendingFileRenameOperations") is not null)
                {
                    return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: true, "Windows Update"));
                }

                continue;
            }

            return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: true, "Windows Update"));
        }

        return Task.FromResult(new ActivityEvidence(IsAvailable: true, IsActive: false));
    }
}

internal sealed class CpuLoadSampleProbe : ILoadSampleProbe
{
    private CpuSample? _lastSample;

    public Task<LoadSample> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            throw new InvalidOperationException("GetSystemTimes failed.");
        }

        var now = DateTimeOffset.Now;
        var current = new CpuSample(ToUInt64(idleTime), ToUInt64(kernelTime), ToUInt64(userTime));
        if (_lastSample is null)
        {
            _lastSample = current;
            return Task.FromResult(new LoadSample(now, 0));
        }

        var previous = _lastSample.Value;
        _lastSample = current;
        var idleDelta = current.Idle - previous.Idle;
        var totalDelta = (current.Kernel - previous.Kernel) + (current.User - previous.User);
        return Task.FromResult(new LoadSample(now, totalDelta == 0 ? 0 : Math.Clamp(1d - (double)idleDelta / totalDelta, 0d, 1d)));
    }

    private static ulong ToUInt64(NativeMethods.FileTime fileTime) =>
        ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;

    private readonly record struct CpuSample(ulong Idle, ulong Kernel, ulong User);
}

internal sealed class NetworkTransferLoadSampleProbe : ILoadSampleProbe
{
    private NetworkSample? _lastSample;

    public Task<LoadSample> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.Now;
        var totalBytes = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up)
            .Select(networkInterface => networkInterface.GetIPv4Statistics())
            .Sum(statistics => statistics.BytesReceived + statistics.BytesSent);

        if (_lastSample is null)
        {
            _lastSample = new NetworkSample(now, totalBytes);
            return Task.FromResult(new LoadSample(now, 0));
        }

        var previous = _lastSample.Value;
        _lastSample = new NetworkSample(now, totalBytes);
        var seconds = Math.Max(1, (now - previous.ObservedAt).TotalSeconds);
        return Task.FromResult(new LoadSample(now, Math.Max(0, totalBytes - previous.TotalBytes) / seconds));
    }

    private readonly record struct NetworkSample(DateTimeOffset ObservedAt, long TotalBytes);
}
