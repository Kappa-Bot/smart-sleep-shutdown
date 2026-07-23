using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public sealed class ResourceWorkloadDetector : DetectorBase
{
    private readonly ILoadSampleProbe _load;
    private readonly Func<DateTimeOffset> _now;
    private readonly double _threshold;
    private readonly int _requiredSamples;
    private int _sustainedSamples;

    public ResourceWorkloadDetector()
        : this(
            new CpuLoadSampleProbe(),
            () => DateTimeOffset.Now,
            threshold: 0.35,
            requiredSamples: 2)
    {
    }

    internal ResourceWorkloadDetector(
        ILoadSampleProbe load,
        Func<DateTimeOffset> now,
        double threshold,
        int requiredSamples)
        : base("detector.resource-workload", ProtectionCategory.ResourceWorkload)
    {
        _load = load;
        _now = now;
        _threshold = threshold;
        _requiredSamples = requiredSamples;
    }

    protected override async Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sample = await _load.ReadAsync(cancellationToken).ConfigureAwait(false);
        _sustainedSamples = sample.Value >= _threshold ? _sustainedSamples + 1 : 0;
        var now = _now();
        return _sustainedSamples >= _requiredSamples
            ? Active(ProtectionClass.Temporary, "resource-workload.sustained", "Protection.ResourceWorkloadSustained", now, now.AddMinutes(10))
            : Inactive(now);
    }
}
