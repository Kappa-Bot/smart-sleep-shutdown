using SmartSleepShutdown.Core.Abstractions;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
