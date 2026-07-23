using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public sealed class TransferDetector : DetectorBase
{
    private readonly ILoadSampleProbe _load;
    private readonly Func<DateTimeOffset> _now;
    private readonly double _threshold;
    private readonly int _requiredSamples;
    private int _sustainedSamples;

    public TransferDetector()
        : this(
            new NetworkTransferLoadSampleProbe(),
            () => DateTimeOffset.Now,
            threshold: 64 * 1024,
            requiredSamples: 2)
    {
    }

    internal TransferDetector(
        ILoadSampleProbe load,
        Func<DateTimeOffset> now,
        double threshold,
        int requiredSamples)
        : base("detector.transfer", ProtectionCategory.Transfer)
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
            ? Active(ProtectionClass.Temporary, "transfer.sustained", "Protection.TransferSustained", now, now.AddMinutes(10))
            : Inactive(now);
    }
}
