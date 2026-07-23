using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Abstractions;

public interface ISessionStateProvider
{
    Task<OperationResult<SessionRuntimeState>> ReadAsync(CancellationToken cancellationToken);
}
