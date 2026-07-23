using Hushward.Infrastructure.Input;

namespace Hushward.Infrastructure.Tests.Input;

public sealed class WindowsIdleStateProviderTests
{
    [Fact]
    public async Task Input_detected_when_idle_duration_drops()
    {
        var provider = new WindowsIdleStateProvider(
            new SequenceIdleApi(TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(2)),
            () => new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero));

        var first = await provider.ReadAsync(CancellationToken.None);
        var second = await provider.ReadAsync(CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(first.Value!.UserInputDetected);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value!.UserInputDetected);
    }

    private sealed class SequenceIdleApi : IWindowsIdleApi
    {
        private readonly Queue<TimeSpan> _durations;

        public SequenceIdleApi(params TimeSpan[] durations)
        {
            _durations = new Queue<TimeSpan>(durations);
        }

        public TimeSpan GetIdleDuration() => _durations.Dequeue();
    }
}
