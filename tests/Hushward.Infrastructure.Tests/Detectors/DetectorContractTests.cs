using Hushward.Application.Abstractions;
using Hushward.Core.Protections;
using Hushward.Infrastructure.Detectors;

namespace Hushward.Infrastructure.Tests.Detectors;

public sealed class DetectorContractTests
{
    [Theory]
    [MemberData(nameof(FailingDetectors))]
    public async Task Detector_failure_becomes_unknown_instead_of_throwing(IProtectionDetector detector)
    {
        var result = await detector.ObserveAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ObservationState.Unknown, result.Value!.State);
        Assert.Equal(ProtectionClass.Critical, result.Value.Class);
        Assert.Null(result.Value.FriendlyApplicationLabel);
    }

    [Fact]
    public async Task Sustained_resource_detector_requires_multiple_high_samples()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var detector = new ResourceWorkloadDetector(
            new SequenceLoadProbe(new LoadSample(now, 0.9), new LoadSample(now.AddSeconds(1), 0.9)),
            () => now,
            threshold: 0.35,
            requiredSamples: 2);

        var first = await detector.ObserveAsync(CancellationToken.None);
        var second = await detector.ObserveAsync(CancellationToken.None);

        Assert.Equal(ObservationState.Inactive, first.Value!.State);
        Assert.Equal(ObservationState.Active, second.Value!.State);
        Assert.Equal(ProtectionClass.Temporary, second.Value.Class);
    }

    [Fact]
    public async Task Meeting_detector_does_not_infer_meeting_when_activity_evidence_is_inactive()
    {
        var detector = new MeetingDetector(
            new FixedActivityProbe(new ActivityEvidence(IsAvailable: true, IsActive: false, FriendlyLabel: "Teams")),
            () => new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero));

        var result = await detector.ObserveAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ObservationState.Inactive, result.Value!.State);
    }

    [Fact]
    public async Task Unsafe_process_label_is_sanitized()
    {
        var detector = new ProtectedProcessDetector(
            new FixedActivityProbe(new ActivityEvidence(IsAvailable: true, IsActive: true, FriendlyLabel: "C:\\Users\\Ana\\secret.docx")),
            () => new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero),
            ProtectionClass.Temporary);

        var result = await detector.ObserveAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.FriendlyApplicationLabel);
    }

    public static IEnumerable<object[]> FailingDetectors()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        yield return [new RemoteSessionDetector(() => throw new InvalidOperationException("boom"), () => now)];
        yield return [new MeetingDetector(new ThrowingActivityProbe(), () => now)];
        yield return [new MediaDetector(new ThrowingActivityProbe(), () => now)];
        yield return [new FullscreenDetector(new ThrowingActivityProbe(), () => now)];
        yield return [new ResourceWorkloadDetector(new ThrowingLoadProbe(), () => now, 0.35, 2)];
        yield return [new TransferDetector(new ThrowingLoadProbe(), () => now, 0.35, 2)];
        yield return [new WindowsUpdateDetector(new ThrowingActivityProbe(), () => now)];
        yield return [new ProtectedProcessDetector(new ThrowingActivityProbe(), () => now, ProtectionClass.Temporary)];
    }

    private sealed class FixedActivityProbe : IActivityEvidenceProbe
    {
        private readonly ActivityEvidence _activityEvidence;

        public FixedActivityProbe(ActivityEvidence activityEvidence)
        {
            _activityEvidence = activityEvidence;
        }

        public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(_activityEvidence);
    }

    private sealed class ThrowingActivityProbe : IActivityEvidenceProbe
    {
        public Task<ActivityEvidence> ReadAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
    }

    private sealed class SequenceLoadProbe : ILoadSampleProbe
    {
        private readonly Queue<LoadSample> _samples;

        public SequenceLoadProbe(params LoadSample[] samples)
        {
            _samples = new Queue<LoadSample>(samples);
        }

        public Task<LoadSample> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(_samples.Dequeue());
    }

    private sealed class ThrowingLoadProbe : ILoadSampleProbe
    {
        public Task<LoadSample> ReadAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
    }
}
