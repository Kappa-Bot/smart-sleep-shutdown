using Hushward.Application.Results;
using Hushward.Core.Actions;

namespace Hushward.Application.Abstractions;

public interface INightActionExecutor
{
    Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken);
}
