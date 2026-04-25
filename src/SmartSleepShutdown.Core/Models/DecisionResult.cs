namespace SmartSleepShutdown.Core.Models;

public sealed record DecisionResult(
    ShutdownDecisionAction Action,
    DecisionState State,
    DateTimeOffset? WarningStartedAt);
