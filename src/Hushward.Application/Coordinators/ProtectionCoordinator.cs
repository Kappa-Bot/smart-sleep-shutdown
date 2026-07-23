using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Protections;

namespace Hushward.Application.Coordinators;

public sealed class ProtectionCoordinator
{
    private readonly IReadOnlyList<IProtectionDetector> _detectors;
    private readonly TimeSpan _perDetectorTimeout;
    private readonly TimeSpan _freshnessWindow;

    public ProtectionCoordinator(IReadOnlyList<IProtectionDetector> detectors)
        : this(detectors, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(10))
    {
    }

    public ProtectionCoordinator(
        IReadOnlyList<IProtectionDetector> detectors,
        TimeSpan perDetectorTimeout,
        TimeSpan freshnessWindow)
    {
        _detectors = detectors;
        _perDetectorTimeout = perDetectorTimeout;
        _freshnessWindow = freshnessWindow;
    }

    public async Task<OperationResult<ProtectionObservation>> ObserveAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var observations = new List<ProtectionSignal>();
        var health = new List<ProtectionDetectorHealth>();
        foreach (var detector in _detectors)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var observeTask = Task.Run(() => detector.ObserveAsync(timeout.Token), CancellationToken.None);
            var completed = await Task.WhenAny(observeTask, Task.Delay(_perDetectorTimeout, cancellationToken)).ConfigureAwait(false);
            if (completed != observeTask)
            {
                await timeout.CancelAsync().ConfigureAwait(false);
                observations.Add(ProtectionSignal.Unknown(detector.Id, ProtectionCategory.SystemTransition, now, "detector.timeout"));
                health.Add(new ProtectionDetectorHealth(detector.Id, Healthy: false, "detector.timeout", now));
                continue;
            }

            var result = await observeTask.ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                var code = result.Error?.Code ?? "detector.failure";
                observations.Add(ProtectionSignal.Unknown(detector.Id, ProtectionCategory.SystemTransition, now, code));
                health.Add(new ProtectionDetectorHealth(detector.Id, Healthy: false, code, now));
                continue;
            }

            if (result.Value.ObservedAt < now.Subtract(_freshnessWindow))
            {
                observations.Add(ProtectionSignal.Unknown(detector.Id, result.Value.Category, now, "detector.stale"));
                health.Add(new ProtectionDetectorHealth(detector.Id, Healthy: false, "detector.stale", now));
                continue;
            }

            observations.Add(result.Value);
            health.Add(new ProtectionDetectorHealth(detector.Id, Healthy: true, "detector.ok", now));
        }

        return OperationResult<ProtectionObservation>.Success(new ProtectionObservation(
            ProtectionPolicy.Summarize(observations, now),
            health.AsReadOnly()));
    }
}

public sealed record ProtectionObservation(
    ProtectionSummary Summary,
    IReadOnlyList<ProtectionDetectorHealth> Health);

public sealed record ProtectionDetectorHealth(
    string DetectorId,
    bool Healthy,
    string Code,
    DateTimeOffset ObservedAt);
