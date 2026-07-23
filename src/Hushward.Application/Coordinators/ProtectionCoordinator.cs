using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Protections;

namespace Hushward.Application.Coordinators;

public sealed class ProtectionCoordinator
{
    private readonly IReadOnlyList<IProtectionDetector> _detectors;

    public ProtectionCoordinator(IReadOnlyList<IProtectionDetector> detectors)
    {
        _detectors = detectors;
    }

    public async Task<OperationResult<ProtectionSummary>> ObserveAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var observations = new List<ProtectionSignal>();
        foreach (var detector in _detectors)
        {
            var result = await detector.ObserveAsync(cancellationToken).ConfigureAwait(false);
            observations.Add(result.IsSuccess && result.Value is not null
                ? result.Value
                : ProtectionSignal.Unknown(detector.Id, ProtectionCategory.SystemTransition, now, result.Error?.Code ?? "detector.failure"));
        }

        return OperationResult<ProtectionSummary>.Success(ProtectionPolicy.Summarize(observations, now));
    }
}
