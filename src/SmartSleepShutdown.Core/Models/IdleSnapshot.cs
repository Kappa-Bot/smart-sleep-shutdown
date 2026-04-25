namespace SmartSleepShutdown.Core.Models;

public sealed record IdleSnapshot(
    DateTimeOffset Now,
    TimeSpan IdleDuration,
    bool InputDetected);
