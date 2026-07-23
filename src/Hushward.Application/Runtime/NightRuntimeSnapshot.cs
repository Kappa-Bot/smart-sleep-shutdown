using Hushward.Core.Decisions;
using Hushward.Core.Protections;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.Application.Runtime;

public sealed record NightRuntimeSnapshot(
    long Sequence,
    DateTimeOffset CapturedAt,
    RuntimeState MonitoringState,
    EffectiveNightPlan? EffectivePlan,
    NightRoutine? ActiveRoutine,
    NightWindow? CurrentWindow,
    IdleRuntimeState IdleState,
    PowerRuntimeState PowerState,
    SessionRuntimeState SessionState,
    ProtectionSummary ProtectionSummary,
    IReadOnlyList<DetectorHealth> DetectorHealth,
    NightDecision? Decision,
    DecisionReasonCode? PrimaryReason,
    IReadOnlyList<DecisionReasonCode> SupportingReasons,
    WarningState WarningState,
    ActionExecutionState ActionExecutionState,
    ScheduleHealth WakeScheduleHealth,
    PersistenceHealth PersistenceHealth,
    UpdateState UpdateState,
    DateTimeOffset? NextEvaluationAt,
    RuntimeEvent? LastMeaningfulEvent)
{
    private IReadOnlyList<DetectorHealth> _detectorHealth = Array.AsReadOnly(DetectorHealth.ToArray());
    private IReadOnlyList<DecisionReasonCode> _supportingReasons = Array.AsReadOnly(SupportingReasons.ToArray());

    public IReadOnlyList<DetectorHealth> DetectorHealth
    {
        get => _detectorHealth;
        init => _detectorHealth = Array.AsReadOnly(value.ToArray());
    }

    public IReadOnlyList<DecisionReasonCode> SupportingReasons
    {
        get => _supportingReasons;
        init => _supportingReasons = Array.AsReadOnly(value.ToArray());
    }

    public static NightRuntimeSnapshot Empty(long sequence, DateTimeOffset capturedAt) => new(
        sequence,
        capturedAt,
        RuntimeState.Disabled,
        null,
        null,
        null,
        new IdleRuntimeState(TimeSpan.Zero, UserInputDetected: false, capturedAt),
        new PowerRuntimeState(IsOnBattery: false, null, IsCharging: false, new HashSet<Core.Actions.NightAction>()),
        new SessionRuntimeState(IsLocked: false, IsRemote: false, null),
        ProtectionPolicy.Summarize([], capturedAt),
        [],
        null,
        null,
        [],
        WarningState.None,
        ActionExecutionState.None,
        new ScheduleHealth(IsHealthy: true, null, null),
        new PersistenceHealth(IsHealthy: true, null),
        new UpdateState(IsChecking: false, UpdateAvailable: false, null),
        null,
        null);

    public bool IsNewerThan(NightRuntimeSnapshot other) => Sequence > other.Sequence;

    public bool IsStaleAt(DateTimeOffset now, TimeSpan maximumAge) =>
        now - CapturedAt > maximumAge;
}
