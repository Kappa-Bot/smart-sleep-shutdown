using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Abstractions;

public interface IHistoryStore
{
    Task<OperationResult<Unit>> AppendAsync(RuntimeEvent runtimeEvent, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<RuntimeEvent>>> ReadRecentAsync(CancellationToken cancellationToken);
}
