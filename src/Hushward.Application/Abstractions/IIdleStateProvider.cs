using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Abstractions;

public interface IIdleStateProvider
{
    Task<OperationResult<IdleRuntimeState>> ReadAsync(CancellationToken cancellationToken);
}
