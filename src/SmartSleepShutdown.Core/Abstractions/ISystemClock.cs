namespace SmartSleepShutdown.Core.Abstractions;

public interface ISystemClock
{
    DateTimeOffset Now { get; }
}
