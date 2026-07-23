namespace SmartSleepShutdown.Core.Models;

public enum DecisionState
{
    Disabled,
    WaitingForWindow,
    Monitoring,
    IdleCandidate,
    WarningCountdown,
    CancelledUntilActivityReset,
    PausedToday,
    ShutdownBlocked,
    ShutdownReady,
    ShutdownIssued,

    Warning = WarningCountdown,
    CancelledAwaitingRearm = CancelledUntilActivityReset
}
