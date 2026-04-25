using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Core.Abstractions;

public interface IContextDetector
{
    ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken);
}
