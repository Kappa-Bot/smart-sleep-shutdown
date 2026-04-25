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
        Assert.Contains(snapshot.Blockers, blocker => blocker.Type == BlockingContextType.DetectorFailure);
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
