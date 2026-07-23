using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public sealed class ProtectedProcessDetector : DetectorBase
{
    private readonly IActivityEvidenceProbe _process;
    private readonly Func<DateTimeOffset> _now;
    private readonly ProtectionClass _protectionClass;

    public ProtectedProcessDetector()
        : this([])
    {
    }

    public ProtectedProcessDetector(IReadOnlyList<ProtectedProcessRule> rules)
        : this(new ProtectedProcessActivityProbe(rules), () => DateTimeOffset.Now, DominantClass(rules))
    {
    }

    internal ProtectedProcessDetector(
        IActivityEvidenceProbe process,
        Func<DateTimeOffset> now,
        ProtectionClass protectionClass)
        : base("detector.protected-process", ProtectionCategory.UserSelectedProcess)
    {
        _process = process;
        _now = now;
        _protectionClass = protectionClass;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _now();
        var evidence = await _process.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!evidence.IsAvailable)
        {
            return Unknown(now, "protected-process.evidence-unavailable");
        }

        return evidence.IsActive
            ? Active(_protectionClass, "protected-process.active", "Protection.ProtectedProcessActive", now, now.AddMinutes(10), evidence.FriendlyLabel)
            : Inactive(now);
    }

    private static ProtectionClass DominantClass(IReadOnlyList<ProtectedProcessRule> rules) =>
        rules.Any(rule => rule.ProtectionClass == ProtectionClass.Critical)
            ? ProtectionClass.Critical
            : ProtectionClass.Temporary;
}
