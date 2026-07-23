namespace Hushward.Core.Decisions;

public enum DecisionReasonCode
{
    RoutineDisabled,
    OutsideNightWindow,
    DayNotSelected,
    OperationInProgress,
    IdleThresholdNotMet,
    CriticalProtectionActive,
    TemporaryProtectionActive,
    RequiredEvidenceUnknown,
    ActionUnsupported,
    ManualConfirmationRequired,
    LatestDecisionReached,
    AuthorizedAlternativeSelected,
    Ready,
    WarningCancelledByInput,
    WarningCancelledByProtection,
    FinalCheckFailed
}
