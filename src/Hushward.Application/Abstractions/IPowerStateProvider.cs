using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Abstractions;

public interface IPowerStateProvider
{
    Task<OperationResult<PowerRuntimeState>> ReadAsync(CancellationToken cancellationToken);
}
