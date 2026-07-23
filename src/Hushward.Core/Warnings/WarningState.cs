namespace Hushward.Core.Warnings;

public enum WarningStateKind
{
    None,
    Active,
    CountdownElapsed,
    CancelledAwaitingFreshIdle
}

public sealed record WarningState(
    WarningStateKind Kind,
    DateTimeOffset? StartedAt)
{
    public static WarningState None { get; } = new(WarningStateKind.None, null);

    public static WarningState Active(DateTimeOffset startedAt) => new(WarningStateKind.Active, startedAt);

    public static WarningState CountdownElapsed(DateTimeOffset startedAt) => new(WarningStateKind.CountdownElapsed, startedAt);

    public static WarningState CancelledAwaitingFreshIdle(DateTimeOffset startedAt) => new(WarningStateKind.CancelledAwaitingFreshIdle, startedAt);
}
