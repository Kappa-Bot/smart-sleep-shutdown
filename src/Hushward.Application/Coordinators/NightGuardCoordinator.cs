using Hushward.Application.Runtime;
using Hushward.Core.Decisions;

namespace Hushward.Application.Coordinators;

public sealed class NightGuardCoordinator
{
    private readonly RuntimeSnapshotPublisher _publisher;
    private readonly SemaphoreSlim _commitGate = new(1, 1);
    private readonly Func<NightRuntimeSnapshot, NightDecision>? _finalEvaluator;

    public NightGuardCoordinator(
        RuntimeSnapshotPublisher publisher,
        Func<NightRuntimeSnapshot, NightDecision>? finalEvaluator = null)
    {
        _publisher = publisher;
        _finalEvaluator = finalEvaluator;
    }

    public async Task<NightRuntimeSnapshot> CommitAsync(
        RuntimeState state,
        CancellationToken cancellationToken)
    {
        await _commitGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var latest = _publisher.Latest;
            var next = latest with
            {
                Sequence = latest.Sequence + 1,
                CapturedAt = DateTimeOffset.UtcNow,
                MonitoringState = state
            };
            _publisher.Publish(next);
            return next;
        }
        finally
        {
            _commitGate.Release();
        }
    }

    public async Task<NightDecision> EvaluateFinalAsync(
        long expectedSequence,
        CancellationToken cancellationToken)
    {
        await _commitGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var latest = _publisher.Latest;
            if (latest.Sequence != expectedSequence)
            {
                return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.FinalCheckFailed);
            }

            var decision = _finalEvaluator?.Invoke(latest) ??
                latest.Decision ??
                NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.FinalCheckFailed);
            var next = latest with
            {
                Sequence = latest.Sequence + 1,
                CapturedAt = DateTimeOffset.UtcNow,
                Decision = decision,
                PrimaryReason = decision.PrimaryReason,
                SupportingReasons = decision.SupportingReasons,
                ActionExecutionState = decision.Kind == NightDecisionKind.AuthorizedToExecute
                    ? ActionExecutionState.Pending
                    : ActionExecutionState.None
            };

            _publisher.Publish(next);
            return decision;
        }
        finally
        {
            _commitGate.Release();
        }
    }
}
