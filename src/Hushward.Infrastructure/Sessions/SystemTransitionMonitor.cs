namespace Hushward.Infrastructure.Sessions;

public sealed record SystemTransitionSnapshot(
    DateTimeOffset? LastLockTransitionAt,
    DateTimeOffset? LastPowerTransitionAt,
    DateTimeOffset? LastDisplayTopologyTransitionAt);

public sealed class SystemTransitionMonitor
{
    private DateTimeOffset? _lastLockTransitionAt;
    private DateTimeOffset? _lastPowerTransitionAt;
    private DateTimeOffset? _lastDisplayTopologyTransitionAt;

    public void RecordLockTransition(DateTimeOffset occurredAt) => _lastLockTransitionAt = occurredAt;

    public void RecordPowerTransition(DateTimeOffset occurredAt) => _lastPowerTransitionAt = occurredAt;

    public void RecordDisplayTopologyTransition(DateTimeOffset occurredAt) => _lastDisplayTopologyTransitionAt = occurredAt;

    public SystemTransitionSnapshot Snapshot() =>
        new(_lastLockTransitionAt, _lastPowerTransitionAt, _lastDisplayTopologyTransitionAt);
}
