using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Protections;
using System.Collections.Frozen;

namespace Hushward.Application.Runtime;

public enum RuntimeState
{
    Disabled,
    WaitingForWindow,
    Monitoring,
    Protected,
    Warning,
    Executing,
    SafeMode
}

public sealed record IdleRuntimeState(
    TimeSpan IdleDuration,
    bool UserInputDetected,
    DateTimeOffset ObservedAt);

public sealed record PowerRuntimeState(
    bool IsOnBattery,
    int? BatteryPercent,
    bool IsCharging,
    IReadOnlySet<NightAction> SupportedActions)
{
    private IReadOnlySet<NightAction> _supportedActions = SupportedActions.ToFrozenSet();

    public IReadOnlySet<NightAction> SupportedActions
    {
        get => _supportedActions;
        init => _supportedActions = value.ToFrozenSet();
    }
}

public sealed record SessionRuntimeState(
    bool IsLocked,
    bool IsRemote,
    DateTimeOffset? LastTransitionAt);

public sealed record DetectorHealth(
    string DetectorId,
    bool Healthy,
    ProtectionCategory Category,
    string? LastErrorCode);

public enum ActionExecutionState
{
    None,
    Pending,
    Executing,
    Succeeded,
    Failed
}

public sealed record ScheduleHealth(
    bool IsHealthy,
    DateTimeOffset? NextRunAt,
    string? LastErrorCode);

public sealed record PersistenceHealth(
    bool IsHealthy,
    string? LastErrorCode);

public sealed record UpdateState(
    bool IsChecking,
    bool UpdateAvailable,
    string? LastErrorCode);

public sealed record RuntimeEvent(
    string Code,
    DateTimeOffset OccurredAt,
    DecisionReasonCode? ReasonCode);
