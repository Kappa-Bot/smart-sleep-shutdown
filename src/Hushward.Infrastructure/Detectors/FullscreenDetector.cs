using Hushward.Core.Protections;
using Hushward.Infrastructure.System;

namespace Hushward.Infrastructure.Detectors;

public sealed class FullscreenDetector : DetectorBase
{
    private readonly IActivityEvidenceProbe _fullscreen;
    private readonly Func<DateTimeOffset> _now;

    public FullscreenDetector()
        : this(new ContextProbeActivityEvidenceProbe(new ForegroundFullscreenContextProbe(), "Pantalla completa"), () => DateTimeOffset.Now)
    {
    }

    internal FullscreenDetector(IActivityEvidenceProbe fullscreen, Func<DateTimeOffset> now)
        : base("detector.fullscreen", ProtectionCategory.FullscreenOrPresentation)
    {
        _fullscreen = fullscreen;
        _now = now;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        var evidence = await _fullscreen.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!evidence.IsAvailable)
        {
            return Unknown(now, "fullscreen.evidence-unavailable");
        }

        return evidence.IsActive
            ? Active(ProtectionClass.Temporary, "fullscreen.active", "Protection.FullscreenActive", now, now.AddMinutes(10), evidence.FriendlyLabel)
            : Inactive(now);
    }
}
