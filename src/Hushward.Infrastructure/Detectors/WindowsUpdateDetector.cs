using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public sealed class WindowsUpdateDetector : DetectorBase
{
    private readonly IActivityEvidenceProbe _update;
    private readonly Func<DateTimeOffset> _now;

    public WindowsUpdateDetector()
        : this(new WindowsUpdateActivityProbe(), () => DateTimeOffset.Now)
    {
    }

    internal WindowsUpdateDetector(IActivityEvidenceProbe update, Func<DateTimeOffset> now)
        : base("detector.windows-update", ProtectionCategory.WindowsUpdate)
    {
        _update = update;
        _now = now;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        var evidence = await _update.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!evidence.IsAvailable)
        {
            return Unknown(now, "windows-update.evidence-unavailable");
        }

        return evidence.IsActive
            ? Active(ProtectionClass.Critical, "windows-update.active", "Protection.WindowsUpdateActive", now)
            : Inactive(now);
    }
}
