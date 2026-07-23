namespace SmartSleepShutdown.Core.Models;

public enum DecisionTransitionReason
{
    None,
    Disabled,
    PausedToday,
    WaitingForStartTime,
    RecentInput,
    IdleThresholdNotMet,
    IdleCandidate,
    SoftContextBlocked,
    DetectorFailureBlocked,
    WarningStarted,
    WarningContinuing,
    WarningCancelledByInput,
    WarningCancelledBySettings,
    WarningCancelledByFinalRecheck,
    ActivityResetRearmed,
    ShutdownReady,
    ShutdownIssued
}
