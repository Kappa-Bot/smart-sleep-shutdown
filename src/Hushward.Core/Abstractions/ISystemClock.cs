namespace Hushward.Core.Abstractions;

public interface ISystemClock
{
    DateTimeOffset Now { get; }
}

