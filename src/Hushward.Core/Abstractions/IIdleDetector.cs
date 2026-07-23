using Hushward.Core.Models;

namespace Hushward.Core.Abstractions;

public interface IIdleDetector
{
    ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken);
}

