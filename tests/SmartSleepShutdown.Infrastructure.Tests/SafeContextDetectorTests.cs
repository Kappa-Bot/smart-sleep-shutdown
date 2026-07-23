using SmartSleepShutdown.Core.Models;
using SmartSleepShutdown.Infrastructure.System;
using System.Runtime.InteropServices;

namespace SmartSleepShutdown.Infrastructure.Tests;

public sealed class SafeContextDetectorTests
{
    [Fact]
    public async Task DetectorFailureReturnsBlockingContext()
    {
        var detector = new AggregateContextDetector(new IContextProbe[] { new ThrowingProbe() });

        var snapshot = await detector.GetCurrentContextAsync(CancellationToken.None);

        Assert.True(snapshot.HasBlockingContext);
        Assert.Contains(snapshot.Blockers, blocker =>
            blocker.Type == BlockingContextType.DetectorFailure
            && blocker.Severity == BlockingContextSeverity.Hard);
    }

    [Fact]
    public void SustainedSignalGateIgnoresSingleTransientSample()
    {
        var gate = new SustainedSignalGate(requiredActiveSamples: 2, requiredClearSamples: 2, staleAfter: TimeSpan.FromMinutes(1));
        var now = new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero);

        Assert.False(gate.Observe(isActiveSample: true, now));
        Assert.True(gate.Observe(isActiveSample: true, now.AddSeconds(5)));
        Assert.True(gate.Observe(isActiveSample: false, now.AddSeconds(10)));
        Assert.False(gate.Observe(isActiveSample: false, now.AddSeconds(15)));
    }

    [Fact]
    public void KnownProcessClassifierReturnsCategory()
    {
        var match = KnownProcessContextProbe.TryClassifyProcessName("Zoom");

        Assert.NotNull(match);
        Assert.Equal(BlockingContextCategory.CallOrMeeting, match.Value.Category);
        Assert.Equal(BlockingContextType.KnownProcess, match.Value.Type);
    }

    [Fact]
    public void MissingAudioEndpointIsTreatedAsNoAudio()
    {
        var exception = new COMException("No endpoint", unchecked((int)0x80070490));

        Assert.True(AudioPlayingContextProbe.IsExpectedNoAudioDeviceFailure(exception));
    }

    private sealed class ThrowingProbe : IContextProbe
    {
        public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("probe failed");
        }
    }
}
