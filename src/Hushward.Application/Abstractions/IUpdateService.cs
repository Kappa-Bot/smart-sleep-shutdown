using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Abstractions;

public interface IUpdateService
{
    Task<OperationResult<UpdateState>> CheckAsync(CancellationToken cancellationToken);

    Task<OperationResult<Unit>> InstallAsync(CancellationToken cancellationToken);
}
