using Hushward.Application.Results;
using Hushward.Application.History;

namespace Hushward.Application.Abstractions;

public interface IHistoryStore
{
    Task<OperationResult<Unit>> AppendAsync(HistoryEvent historyEvent, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> ReplaceLastAsync(HistoryEvent historyEvent, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> PruneBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<HistoryEvent>>> ReadRecentAsync(int maxCount, CancellationToken cancellationToken);
}
