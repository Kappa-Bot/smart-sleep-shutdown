using Hushward.Infrastructure.Power;

namespace Hushward.Infrastructure.Tests.Power;

public sealed class WindowsPowerStateProviderTests
{
    [Fact]
    public async Task Power_state_tracks_transition_when_ac_state_changes()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var api = new SequencePowerApi(
            new WindowsPowerLineStatus(false, 90, true),
            new WindowsPowerLineStatus(true, 88, false));
        var provider = new WindowsPowerStateProvider(api, () => now);

        var first = await provider.ReadAsync(CancellationToken.None);
        var second = await provider.ReadAsync(CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Null(first.Value!.LastTransitionAt);
        Assert.True(second.IsSuccess);
        Assert.Equal(now, second.Value!.LastTransitionAt);
        Assert.True(second.Value.IsOnBattery);
    }

    private sealed class SequencePowerApi : IWindowsPowerApi
    {
        private readonly Queue<WindowsPowerLineStatus> _statuses;

        public SequencePowerApi(params WindowsPowerLineStatus[] statuses)
        {
            _statuses = new Queue<WindowsPowerLineStatus>(statuses);
        }

        public WindowsPowerLineStatus ReadLineStatus() => _statuses.Dequeue();

        public WindowsPowerCapabilities ReadCapabilities() => new(true, true);

        public bool SetSuspendState(bool hibernate) => true;
    }
}
