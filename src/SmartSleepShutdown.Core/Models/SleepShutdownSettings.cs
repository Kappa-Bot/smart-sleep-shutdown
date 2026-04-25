namespace SmartSleepShutdown.Core.Models;

public sealed record SleepShutdownSettings(
    bool Enabled,
    TimeOnly StartTime,
    TimeSpan IdleThreshold,
    TimeSpan WarningDuration,
    bool ContextChecksEnabled)
{
    public static SleepShutdownSettings Default { get; } = new(
        Enabled: false,
        StartTime: new TimeOnly(1, 0),
        IdleThreshold: TimeSpan.FromMinutes(15),
        WarningDuration: TimeSpan.FromSeconds(60),
        ContextChecksEnabled: true);
}
