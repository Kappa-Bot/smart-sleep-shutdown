namespace Hushward.Application.History;

public enum HistoryEventKind
{
    RoutineEvaluated,
    WaitingReasonChanged,
    ProtectionActivated,
    ProtectionCleared,
    WarningStarted,
    WarningCancelled,
    ActionExecuted,
    ActionFailed,
    TaskSynchronizationChanged,
    DetectorDegraded,
    DetectorRecovered,
    ConfigurationMigrated,
    ConfigurationRestored
}
