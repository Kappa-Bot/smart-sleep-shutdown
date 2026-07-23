using Hushward.Core.Protections;
using Hushward.Infrastructure.System;

namespace Hushward.Infrastructure.Detectors;

public sealed class MediaDetector : DetectorBase
{
    private readonly IActivityEvidenceProbe _media;
    private readonly Func<DateTimeOffset> _now;

    public MediaDetector()
        : this(new ContextProbeActivityEvidenceProbe(new AudioPlayingContextProbe(), "Audio"), () => DateTimeOffset.Now)
    {
    }

    internal MediaDetector(IActivityEvidenceProbe media, Func<DateTimeOffset> now)
        : base("detector.media", ProtectionCategory.Media)
    {
        _media = media;
        _now = now;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        var evidence = await _media.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!evidence.IsAvailable)
        {
            return Unknown(now, "media.evidence-unavailable");
        }

        return evidence.IsActive
            ? Active(ProtectionClass.Temporary, "media.active", "Protection.MediaActive", now, now.AddMinutes(10), evidence.FriendlyLabel)
            : Inactive(now);
    }
}
