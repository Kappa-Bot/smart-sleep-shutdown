namespace Hushward.Core.Decisions;

public enum NightDecisionKind
{
    Disabled,
    OutsideSchedule,
    WaitingForIdle,
    Protected,
    Degraded,
    CapabilityBlocked,
    ManualConfirmationRequired,
    ReadyToWarn,
    WarningActive,
    AuthorizedToExecute,
    AbandonedForNight
}
