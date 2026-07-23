using Hushward.Infrastructure.Sessions;

namespace Hushward.Infrastructure.Tests.Sessions;

public sealed class SystemTransitionMonitorTests
{
    [Fact]
    public void Snapshot_reports_latest_recorded_transitions()
    {
        var monitor = new SystemTransitionMonitor();
        var lockAt = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
        var powerAt = lockAt.AddMinutes(1);
        var displayAt = lockAt.AddMinutes(2);

        monitor.RecordLockTransition(lockAt);
        monitor.RecordPowerTransition(powerAt);
        monitor.RecordDisplayTopologyTransition(displayAt);

        var snapshot = monitor.Snapshot();

        Assert.Equal(lockAt, snapshot.LastLockTransitionAt);
        Assert.Equal(powerAt, snapshot.LastPowerTransitionAt);
        Assert.Equal(displayAt, snapshot.LastDisplayTopologyTransitionAt);
    }

    [Fact]
    public async Task Session_provider_records_lock_transition()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 30, 0, TimeSpan.Zero);
        var monitor = new SystemTransitionMonitor();
        var provider = new WindowsSessionStateProvider(
            new SequenceSessionApi(new WindowsSessionSnapshot(false, false), new WindowsSessionSnapshot(true, false)),
            monitor,
            () => now);

        await provider.ReadAsync(CancellationToken.None);
        var result = await provider.ReadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsLocked);
        Assert.Equal(now, result.Value.LastLockTransitionAt);
    }

    private sealed class SequenceSessionApi : IWindowsSessionApi
    {
        private readonly Queue<WindowsSessionSnapshot> _snapshots;

        public SequenceSessionApi(params WindowsSessionSnapshot[] snapshots)
        {
            _snapshots = new Queue<WindowsSessionSnapshot>(snapshots);
        }

        public WindowsSessionSnapshot ReadSession() => _snapshots.Dequeue();

        public bool LockWorkStation() => true;
    }
}
