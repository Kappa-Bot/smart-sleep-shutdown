using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Core.Abstractions;

public interface IIdleDetector
{
    ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken);
}
