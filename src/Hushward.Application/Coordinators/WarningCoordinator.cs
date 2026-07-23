using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Warnings;
using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Warnings;

namespace Hushward.Application.Coordinators;

public sealed class WarningCoordinator
{
    private readonly RuntimeSnapshotPublisher _publisher;
    private readonly ActionCoordinator _actionCoordinator;
    private readonly NightGuardCoordinator _nightGuardCoordinator;

    public WarningCoordinator(
        RuntimeSnapshotPublisher publisher,
        ActionCoordinator actionCoordinator,
        NightGuardCoordinator nightGuardCoordinator)
    {
        _publisher = publisher;
        _actionCoordinator = actionCoordinator;
        _nightGuardCoordinator = nightGuardCoordinator;
    }

    public Task InvalidateAsync(WarningInvalidation invalidation)
    {
        var latest = _publisher.Latest;
        if (latest.WarningState.Kind != WarningStateKind.Active)
        {
            return Task.CompletedTask;
        }

        var cancelled = latest with
        {
            Sequence = latest.Sequence + 1,
            CapturedAt = DateTimeOffset.UtcNow,
            MonitoringState = RuntimeState.Monitoring,
            WarningState = WarningState.CancelledAwaitingFreshIdle(latest.WarningState.StartedAt ?? latest.CapturedAt),
            PrimaryReason = invalidation.Kind == WarningInvalidationKind.UserInput
                ? DecisionReasonCode.WarningCancelledByInput
                : DecisionReasonCode.WarningCancelledByProtection,
            LastMeaningfulEvent = new RuntimeEvent(
                invalidation.ReasonCode,
                DateTimeOffset.UtcNow,
                invalidation.Kind == WarningInvalidationKind.UserInput
                    ? DecisionReasonCode.WarningCancelledByInput
                    : DecisionReasonCode.WarningCancelledByProtection)
        };

        _publisher.Publish(cancelled);
        return Task.CompletedTask;
    }

    public async Task<OperationResult<Unit>> CompleteCountdownAsync(
        long expectedSequence,
        NightAction expectedAction,
        CancellationToken cancellationToken)
    {
        var latest = _publisher.Latest;
        if (latest.Sequence != expectedSequence ||
            latest.WarningState.Kind != WarningStateKind.Active)
        {
            return OperationResult<Unit>.Failure("warning.stale", "Warning.Stale");
        }

        var decision = await _nightGuardCoordinator
            .EvaluateFinalAsync(expectedSequence, cancellationToken)
            .ConfigureAwait(false);
        if (decision.Kind != NightDecisionKind.AuthorizedToExecute ||
            decision.AuthorizedAction != expectedAction)
        {
            return OperationResult<Unit>.Failure("warning.final-check-blocked", "Warning.FinalCheckBlocked");
        }

        return await _actionCoordinator.ExecuteOnceAsync(
            expectedSequence,
            expectedAction,
            cancellationToken).ConfigureAwait(false);
    }
}
