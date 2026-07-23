using Hushward.Application.Abstractions;
using Hushward.Application.Results;

namespace Hushward.Application.History;

public sealed class HistoryCoordinator
{
    private readonly IHistoryStore _store;
    private readonly TimeSpan _coalescingInterval;
    private readonly HistoryRetention _retention;

    public HistoryCoordinator(
        IHistoryStore store,
        TimeSpan coalescingInterval)
        : this(store, coalescingInterval, HistoryRetention.Default)
    {
    }

    public HistoryCoordinator(
        IHistoryStore store,
        TimeSpan coalescingInterval,
        HistoryRetention retention)
    {
        _store = store;
        _coalescingInterval = coalescingInterval;
        _retention = retention;
    }

    public async Task<OperationResult<Unit>> RecordAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
    {
        if (!_retention.IsEnabled)
        {
            return OperationResult<Unit>.Success(new Unit());
        }

        var normalized = historyEvent with
        {
            OccurrenceCount = Math.Max(1, historyEvent.OccurrenceCount),
            LastOccurredAt = historyEvent.LastOccurredAt < historyEvent.OccurredAt
                ? historyEvent.OccurredAt
                : historyEvent.LastOccurredAt
        };

        var recent = await _store.ReadRecentAsync(1, cancellationToken).ConfigureAwait(false);
        if (!recent.IsSuccess)
        {
            return OperationResult<Unit>.Failure(
                recent.Error!.Code,
                recent.Error.MessageKey,
                recent.Error.TechnicalDetail);
        }

        var last = recent.Value!.LastOrDefault();
        OperationResult<Unit> writeResult;
        if (last is not null
            && last.IsSemanticallySameAs(normalized)
            && normalized.OccurredAt - last.LastOccurredAt <= _coalescingInterval)
        {
            writeResult = await _store.ReplaceLastAsync(last.CoalesceWith(normalized), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            writeResult = await _store.AppendAsync(normalized, cancellationToken).ConfigureAwait(false);
        }

        if (!writeResult.IsSuccess)
        {
            return writeResult;
        }

        var retentionPeriod = _retention.Period;
        if (retentionPeriod is null)
        {
            return OperationResult<Unit>.Success(new Unit());
        }

        return await _store.PruneBeforeAsync(
            normalized.LastOccurredAt.Subtract(retentionPeriod.Value),
            cancellationToken).ConfigureAwait(false);
    }
}
