using Hushward.Application.Abstractions;
using Hushward.Application.History;
using Hushward.Application.Results;

namespace Hushward.Application.Tests.History;

public sealed class HistoryCoordinatorTests
{
    [Fact]
    public async Task Repeated_identical_waiting_state_is_coalesced()
    {
        var store = new RecordingHistoryStore();
        var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10));
        var evt = TestHistory.Waiting("transfer.sustained");

        await coordinator.RecordAsync(evt, CancellationToken.None);
        await coordinator.RecordAsync(evt with { OccurredAt = evt.OccurredAt.AddMinutes(1), LastOccurredAt = evt.OccurredAt.AddMinutes(1) }, CancellationToken.None);

        Assert.Single(store.Events);
        Assert.Equal(2, store.Events[0].OccurrenceCount);
    }

    [Fact]
    public async Task Different_reason_is_not_coalesced()
    {
        var store = new RecordingHistoryStore();
        var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10));

        await coordinator.RecordAsync(TestHistory.Waiting("not-time-yet"), CancellationToken.None);
        await coordinator.RecordAsync(TestHistory.Waiting("input-recent"), CancellationToken.None);

        Assert.Equal(2, store.Events.Count);
    }

    [Fact]
    public async Task Retention_off_does_not_write_history()
    {
        var store = new RecordingHistoryStore();
        var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10), HistoryRetention.Off);

        await coordinator.RecordAsync(TestHistory.Waiting("not-time-yet"), CancellationToken.None);

        Assert.Empty(store.Events);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(7, true)]
    [InlineData(14, true)]
    [InlineData(30, true)]
    [InlineData(1, false)]
    public void Retention_accepts_only_approved_values(int? days, bool expected)
    {
        var result = HistoryRetention.TryFromDays(days, out var retention);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.Equal(days, retention.Days);
        }
    }

    [Theory]
    [InlineData("C:\\Users\\Ana\\secret.txt")]
    [InlineData("https://example.test/private")]
    [InlineData("secret.docx")]
    [InlineData("--token abc123")]
    public async Task Unsafe_friendly_labels_are_not_persisted(string unsafeLabel)
    {
        var store = new RecordingHistoryStore();
        var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10));
        var evt = new HistoryEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero),
            HistoryEventKind.ProtectionActivated,
            "proteccion.activa",
            "media",
            unsafeLabel,
            1,
            new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero));

        await coordinator.RecordAsync(evt, CancellationToken.None);

        Assert.Null(store.Events[0].FriendlyApplicationLabel);
    }

    [Fact]
    public async Task Unsafe_label_cannot_be_reintroduced_with_expression()
    {
        var store = new RecordingHistoryStore();
        var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10));
        var evt = TestHistory.Waiting("proteccion.activa") with
        {
            FriendlyApplicationLabel = "C:\\Users\\Ana\\secret.txt"
        };

        await coordinator.RecordAsync(evt, CancellationToken.None);

        Assert.Null(store.Events[0].FriendlyApplicationLabel);
    }

    private sealed class RecordingHistoryStore : IHistoryStore
    {
        public List<HistoryEvent> Events { get; } = [];

        public Task<OperationResult<Unit>> AppendAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
        {
            Events.Add(historyEvent);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }

        public Task<OperationResult<Unit>> ReplaceLastAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
        {
            Events[^1] = historyEvent;
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }

        public Task<OperationResult<Unit>> PruneBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
        {
            Events.RemoveAll(historyEvent => historyEvent.LastOccurredAt < cutoff);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }

        public Task<OperationResult<IReadOnlyList<HistoryEvent>>> ReadRecentAsync(int maxCount, CancellationToken cancellationToken)
        {
            IReadOnlyList<HistoryEvent> recent = Events.TakeLast(maxCount).ToArray();
            return Task.FromResult(OperationResult<IReadOnlyList<HistoryEvent>>.Success(recent));
        }
    }

    private static class TestHistory
    {
        public static HistoryEvent Waiting(string reasonCode)
        {
            var now = new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero);
            return HistoryEvent.Create(now, HistoryEventKind.WaitingReasonChanged, reasonCode);
        }
    }
}
