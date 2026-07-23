using System.Text.Json;
using Hushward.Application.Abstractions;
using Hushward.Application.History;
using Hushward.Application.Results;

namespace Hushward.Infrastructure.History;

public sealed class JsonLinesHistoryStore : IHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;

    public JsonLinesHistoryStore(string path)
    {
        _path = path;
    }

    public async Task<OperationResult<Unit>> AppendAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
    {
        try
        {
            EnsureDirectory();
            var line = JsonSerializer.Serialize(historyEvent, JsonOptions);
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
            return OperationResult<Unit>.Success(new Unit());
        }
        catch (IOException ex)
        {
            return OperationResult<Unit>.Failure("history.append-failed", "History.AppendFailed", ex.Message);
        }
    }

    public async Task<OperationResult<Unit>> ReplaceLastAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
    {
        var events = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        if (!events.IsSuccess)
        {
            return OperationResult<Unit>.Failure(events.Error!.Code, events.Error.MessageKey, events.Error.TechnicalDetail);
        }

        var values = events.Value!;
        if (values.Count == 0)
        {
            return await AppendAsync(historyEvent, cancellationToken).ConfigureAwait(false);
        }

        values[^1] = historyEvent;
        return await WriteAllAsync(values, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OperationResult<Unit>> PruneBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var events = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        if (!events.IsSuccess)
        {
            return OperationResult<Unit>.Failure(events.Error!.Code, events.Error.MessageKey, events.Error.TechnicalDetail);
        }

        var retained = events.Value!
            .Where(historyEvent => historyEvent.LastOccurredAt >= cutoff)
            .ToList();

        return retained.Count == events.Value!.Count
            ? OperationResult<Unit>.Success(new Unit())
            : await WriteAllAsync(retained, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OperationResult<IReadOnlyList<HistoryEvent>>> ReadRecentAsync(int maxCount, CancellationToken cancellationToken)
    {
        var events = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        if (!events.IsSuccess)
        {
            return OperationResult<IReadOnlyList<HistoryEvent>>.Failure(
                events.Error!.Code,
                events.Error.MessageKey,
                events.Error.TechnicalDetail);
        }

        var recent = events.Value!
            .OrderBy(historyEvent => historyEvent.LastOccurredAt)
            .TakeLast(Math.Max(0, maxCount))
            .ToArray();

        return OperationResult<IReadOnlyList<HistoryEvent>>.Success(recent);
    }

    private async Task<OperationResult<List<HistoryEvent>>> ReadAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_path))
            {
                return OperationResult<List<HistoryEvent>>.Success([]);
            }

            var events = new List<HistoryEvent>();
            foreach (var line in await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var historyEvent = JsonSerializer.Deserialize<HistoryEvent>(line, JsonOptions);
                if (historyEvent is null)
                {
                    return OperationResult<List<HistoryEvent>>.Failure(
                        "history.invalid",
                        "History.Invalid");
                }

                events.Add(historyEvent);
            }

            return OperationResult<List<HistoryEvent>>.Success(events);
        }
        catch (JsonException ex)
        {
            return OperationResult<List<HistoryEvent>>.Failure("history.invalid", "History.Invalid", ex.Message);
        }
        catch (IOException ex)
        {
            return OperationResult<List<HistoryEvent>>.Failure("history.read-failed", "History.ReadFailed", ex.Message);
        }
    }

    private async Task<OperationResult<Unit>> WriteAllAsync(IReadOnlyList<HistoryEvent> historyEvents, CancellationToken cancellationToken)
    {
        try
        {
            EnsureDirectory();
            var directory = global::System.IO.Path.GetDirectoryName(_path) ?? ".";
            var tempPath = global::System.IO.Path.Combine(directory, "history.tmp.jsonl");
            var lines = historyEvents.Select(historyEvent => JsonSerializer.Serialize(historyEvent, JsonOptions));
            await File.WriteAllLinesAsync(tempPath, lines, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _path, overwrite: true);
            return OperationResult<Unit>.Success(new Unit());
        }
        catch (IOException ex)
        {
            return OperationResult<Unit>.Failure("history.write-failed", "History.WriteFailed", ex.Message);
        }
    }

    private void EnsureDirectory()
    {
        var directory = global::System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
