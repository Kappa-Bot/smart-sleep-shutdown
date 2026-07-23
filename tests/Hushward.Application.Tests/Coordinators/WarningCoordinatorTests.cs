using Hushward.Application.Abstractions;
using Hushward.Application.Coordinators;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Tests.TestSupport;
using Hushward.Application.Warnings;
using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Warnings;

namespace Hushward.Application.Tests.Coordinators;

public sealed class WarningCoordinatorTests
{
    [Fact]
    public async Task StartPublishesSingleActiveWarningSequence()
    {
        var startedAt = DateTimeOffset.Parse("2026-07-23T01:00:00Z");
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12));
        var warning = new WarningCoordinator(
            publisher,
            new ActionCoordinator(new RecordingNightActionExecutor()),
            new NightGuardCoordinator(publisher));

        var first = await warning.StartAsync(NightAction.ShutDown, TimeSpan.FromSeconds(60), startedAt);
        var duplicate = await warning.StartAsync(NightAction.ShutDown, TimeSpan.FromSeconds(60), startedAt);

        Assert.Equal(13, first);
        Assert.Equal(first, duplicate);
        Assert.Equal(WarningStateKind.Active, publisher.Latest.WarningState.Kind);
        Assert.Equal(NightDecisionKind.ReadyToWarn, publisher.Latest.Decision!.Kind);
    }

    [Fact]
    public async Task Input_event_invalidates_active_warning()
    {
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:00:00Z"))
        });
        var nightGuard = new NightGuardCoordinator(
            publisher,
            _ => NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.FinalCheckFailed));
        var warning = new WarningCoordinator(
            publisher,
            new ActionCoordinator(new RecordingNightActionExecutor()),
            nightGuard);

        await warning.InvalidateAsync(new WarningInvalidation(WarningInvalidationKind.UserInput, "input.resumed"));

        Assert.Equal(WarningStateKind.CancelledAwaitingFreshIdle, publisher.Latest.WarningState.Kind);
    }

    [Fact]
    public async Task Final_check_protection_prevents_action_execution()
    {
        var executor = new RecordingNightActionExecutor();
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:00:00Z"))
        });
        var warning = new WarningCoordinator(
            publisher,
            new ActionCoordinator(executor),
            new NightGuardCoordinator(
                publisher,
                _ => NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.CriticalProtectionActive)));

        await warning.CompleteCountdownAsync(12, NightAction.ShutDown, CancellationToken.None);

        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task Authorized_final_check_executes_once()
    {
        var executor = new RecordingNightActionExecutor();
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:00:00Z"))
        });
        var warning = new WarningCoordinator(
            publisher,
            new ActionCoordinator(executor),
            new NightGuardCoordinator(
                publisher,
                _ => NightDecision.Ready(
                    NightAction.ShutDown,
                    DecisionReasonCode.Ready,
                    null,
                    NightDecisionKind.AuthorizedToExecute)));

        await Task.WhenAll(
            warning.CompleteCountdownAsync(12, NightAction.ShutDown, CancellationToken.None),
            warning.CompleteCountdownAsync(12, NightAction.ShutDown, CancellationToken.None));

        Assert.Single(executor.Calls);
    }

    [Fact]
    public async Task Invalidated_warning_sequence_cannot_execute_later()
    {
        var executor = new RecordingNightActionExecutor();
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:00:00Z"))
        });
        var warning = new WarningCoordinator(
            publisher,
            new ActionCoordinator(executor),
            new NightGuardCoordinator(
                publisher,
                _ => NightDecision.Ready(
                    NightAction.ShutDown,
                    DecisionReasonCode.Ready,
                    null,
                    NightDecisionKind.AuthorizedToExecute)));

        await warning.InvalidateAsync(new WarningInvalidation(WarningInvalidationKind.UserInput, "input.resumed"));
        await warning.CompleteCountdownAsync(12, NightAction.ShutDown, CancellationToken.None);

        Assert.Empty(executor.Calls);
    }

    private sealed class RecordingNightActionExecutor : INightActionExecutor
    {
        public List<NightAction> Calls { get; } = [];

        public Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken)
        {
            Calls.Add(action);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }
    }
}
