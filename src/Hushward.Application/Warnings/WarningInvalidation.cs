namespace Hushward.Application.Warnings;

public enum WarningInvalidationKind
{
    UserInput,
    ProtectionActivated,
    PowerTransition,
    SessionTransition,
    SuspendOrResume,
    DisplayTopologyChanged,
    RoutineChanged,
    UpdateOrInstallStarted,
    ApplicationShutdown
}

public sealed record WarningInvalidation(
    WarningInvalidationKind Kind,
    string ReasonCode);
