using Hushward.Core.Abstractions;

namespace Hushward.Infrastructure.System;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

