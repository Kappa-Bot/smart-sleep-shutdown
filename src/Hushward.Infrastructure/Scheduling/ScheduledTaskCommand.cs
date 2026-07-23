namespace Hushward.Infrastructure.Scheduling;

public sealed record ScheduledTaskCommand(
    string FileName,
    IReadOnlyList<string> Arguments);
