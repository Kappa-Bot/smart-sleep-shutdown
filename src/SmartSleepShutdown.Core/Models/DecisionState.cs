namespace SmartSleepShutdown.Core.Models;

public enum DecisionState
{
    Disabled,
    Monitoring,
    Warning,
    CancelledAwaitingRearm,
    ShutdownIssued
}
