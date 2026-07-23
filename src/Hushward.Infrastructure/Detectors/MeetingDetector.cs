using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public sealed class MeetingDetector : DetectorBase
{
    private readonly IActivityEvidenceProbe _activity;
    private readonly Func<DateTimeOffset> _now;

    public MeetingDetector()
        : this(new MeetingActivityEvidenceProbe(), () => DateTimeOffset.Now)
    {
    }

    internal MeetingDetector(IActivityEvidenceProbe activity, Func<DateTimeOffset> now)
        : base("detector.meeting", ProtectionCategory.Meeting)
    {
        _activity = activity;
        _now = now;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        var evidence = await _activity.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!evidence.IsAvailable)
        {
            return Unknown(now, "meeting.evidence-unavailable");
        }

        return evidence.IsActive
            ? Active(ProtectionClass.Critical, "meeting.active", "Protection.MeetingActive", now, friendlyApplicationLabel: evidence.FriendlyLabel)
            : Inactive(now);
    }
}
