using Hushward.Core.Actions;
using Hushward.Core.Protections;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.Core.Decisions;

public sealed record NightEvaluationInput(
    DateTimeOffset EvaluatedAt,
    TimeZoneInfo TimeZone,
    NightRoutine Routine,
    TonightOverride? TonightOverride,
    TimeSpan IdleDuration,
    ProtectionSummary Protections,
    IReadOnlySet<NightAction> SupportedActions,
    WarningState WarningState,
    bool UserInputDetected,
    bool InstallUpdateOrRecoveryActive,
    bool LatestDecisionReached,
    int? BatteryPercent,
    bool BatteryCharging);
