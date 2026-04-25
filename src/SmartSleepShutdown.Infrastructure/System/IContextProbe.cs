using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public interface IContextProbe
{
    ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken);
}
