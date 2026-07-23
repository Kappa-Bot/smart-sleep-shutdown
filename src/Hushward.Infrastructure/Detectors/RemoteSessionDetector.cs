using Hushward.Core.Protections;
using Hushward.Infrastructure.Interop;

namespace Hushward.Infrastructure.Detectors;

public sealed class RemoteSessionDetector : DetectorBase
{
    private readonly Func<bool> _isRemote;
    private readonly Func<DateTimeOffset> _now;

    public RemoteSessionDetector()
        : this(() => NativeMethods.GetSystemMetrics(NativeMethods.SmRemoteSession) != 0, () => DateTimeOffset.Now)
    {
    }

    internal RemoteSessionDetector(Func<bool> isRemote, Func<DateTimeOffset> now)
        : base("detector.remote-session", ProtectionCategory.RemoteSession)
    {
        _isRemote = isRemote;
        _now = now;
    }

    protected override Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        return Task.FromResult(_isRemote()
            ? Active(ProtectionClass.Critical, "remote-session.active", "Protection.RemoteSessionActive", now)
            : Inactive(now));
    }
}
