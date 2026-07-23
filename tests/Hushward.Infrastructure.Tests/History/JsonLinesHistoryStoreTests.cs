using Hushward.Application.History;
using Hushward.Infrastructure.History;
using Hushward.Infrastructure.Tests.TestSupport;

namespace Hushward.Infrastructure.Tests.History;

public sealed class JsonLinesHistoryStoreTests
{
    [Fact]
    public async Task Append_and_read_recent_round_trips_typed_events()
    {
        using var temp = new TemporaryDirectory();
        var store = new JsonLinesHistoryStore(temp.PathOf("history.jsonl"));

        await store.AppendAsync(Event("not-time-yet", 1), CancellationToken.None);
        await store.AppendAsync(Event("input-recent", 2), CancellationToken.None);

        var result = await store.ReadRecentAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("input-recent", result.Value![0].ReasonCode);
    }

    [Fact]
    public async Task Replace_last_rewrites_only_latest_event()
    {
        using var temp = new TemporaryDirectory();
        var store = new JsonLinesHistoryStore(temp.PathOf("history.jsonl"));

        await store.AppendAsync(Event("first", 1), CancellationToken.None);
        await store.AppendAsync(Event("second", 2), CancellationToken.None);
        await store.ReplaceLastAsync(Event("second", 3) with { OccurrenceCount = 4 }, CancellationToken.None);

        var result = await store.ReadRecentAsync(10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["first", "second"], result.Value!.Select(evt => evt.ReasonCode));
        Assert.Equal(4, result.Value![1].OccurrenceCount);
    }

    [Fact]
    public async Task Prune_removes_events_older_than_cutoff()
    {
        using var temp = new TemporaryDirectory();
        var store = new JsonLinesHistoryStore(temp.PathOf("history.jsonl"));

        await store.AppendAsync(Event("old", 1), CancellationToken.None);
        await store.AppendAsync(Event("new", 10), CancellationToken.None);
        await store.PruneBeforeAsync(new DateTimeOffset(2026, 7, 23, 5, 0, 0, TimeSpan.Zero), CancellationToken.None);

        var result = await store.ReadRecentAsync(10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("new", result.Value![0].ReasonCode);
    }

    private static HistoryEvent Event(string reasonCode, int hour) =>
        HistoryEvent.Create(
            new DateTimeOffset(2026, 7, 23, hour, 0, 0, TimeSpan.Zero),
            HistoryEventKind.WaitingReasonChanged,
            reasonCode);
}
