using Hushward.Application.Abstractions;
using Hushward.Application.Coordinators;
using Hushward.Application.Results;
using Hushward.Core.Protections;

namespace Hushward.Application.Tests.Coordinators;

public sealed class ProtectionCoordinatorTests
{
    [Fact]
    public async Task Timed_out_detector_blocks_automatic_action()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var coordinator = new ProtectionCoordinator(
            [new NeverCompletingDetector("detector.never")],
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromSeconds(10));

        var result = await coordinator.ObserveAsync(now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Summary.HasCriticalBlock);
        Assert.Contains(result.Value.Health, health => health.Code == "detector.timeout");
    }

    [Fact]
    public async Task Synchronously_blocking_detector_cannot_bypass_timeout()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var coordinator = new ProtectionCoordinator(
            [new BlockingDetector("detector.blocking", TimeSpan.FromMilliseconds(200))],
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromSeconds(10));

        var started = DateTimeOffset.UtcNow;
        var result = await coordinator.ObserveAsync(now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(DateTimeOffset.UtcNow - started < TimeSpan.FromMilliseconds(150));
        Assert.Contains(result.Value!.Health, health => health.Code == "detector.timeout");
    }

    [Fact]
    public async Task Stale_detector_snapshot_blocks_automatic_action()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var coordinator = new ProtectionCoordinator(
            [new FixedDetector(Signal("detector.stale", ObservationState.Inactive, now.AddMinutes(-5)))],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = await coordinator.ObserveAsync(now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Summary.HasCriticalBlock);
        Assert.Contains(result.Value.Health, health => health.Code == "detector.stale");
    }

    [Fact]
    public async Task Detector_failure_result_blocks_as_unknown()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var coordinator = new ProtectionCoordinator(
            [new FailingDetector("detector.failed")],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = await coordinator.ObserveAsync(now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Summary.HasCriticalBlock);
        Assert.Contains(result.Value.Health, health => health.Code == "detector.failure");
    }

    [Fact]
    public async Task Healthy_temporary_signal_remains_temporary()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var coordinator = new ProtectionCoordinator(
            [new FixedDetector(Signal("detector.media", ObservationState.Active, now, ProtectionClass.Temporary))],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = await coordinator.ObserveAsync(now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Summary.HasCriticalBlock);
        Assert.True(result.Value.Summary.HasTemporaryBlock);
        Assert.Contains(result.Value.Health, health => health.Code == "detector.ok");
    }

    private static ProtectionSignal Signal(
        string detectorId,
        ObservationState state,
        DateTimeOffset observedAt,
        ProtectionClass protectionClass = ProtectionClass.Contextual) =>
        new(
            detectorId,
            ProtectionCategory.Media,
            protectionClass,
            state,
            "media.active",
            "Protection.MediaActive",
            observedAt,
            null,
            null);

    private sealed class FixedDetector : IProtectionDetector
    {
        private readonly ProtectionSignal _signal;

        public FixedDetector(ProtectionSignal signal)
        {
            _signal = signal;
        }

        public string Id => _signal.DetectorId;

        public Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<ProtectionSignal>.Success(_signal));
    }

    private sealed class FailingDetector : IProtectionDetector
    {
        public FailingDetector(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<ProtectionSignal>.Failure("detector.failure", "Detector.Failure"));
    }

    private sealed class NeverCompletingDetector : IProtectionDetector
    {
        public NeverCompletingDetector(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public async Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("unreachable");
        }
    }

    private sealed class BlockingDetector : IProtectionDetector
    {
        private readonly TimeSpan _delay;

        public BlockingDetector(string id, TimeSpan delay)
        {
            Id = id;
            _delay = delay;
        }

        public string Id { get; }

        public Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken)
        {
            Thread.Sleep(_delay);
            return Task.FromResult(OperationResult<ProtectionSignal>.Success(Signal(Id, ObservationState.Inactive, DateTimeOffset.UtcNow)));
        }
    }
}
