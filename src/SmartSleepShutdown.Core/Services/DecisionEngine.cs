using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Core.Services;

public sealed class DecisionEngine
{
    private DateTimeOffset? _warningStartedAt;

    public DecisionState State { get; private set; } = DecisionState.Disabled;

    public DecisionResult Evaluate(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now)
    {
        if (!settings.Enabled)
        {
            _warningStartedAt = null;
            State = DecisionState.Disabled;
            return Current(ShutdownDecisionAction.None, DecisionTransitionReason.Disabled);
        }

        if (State == DecisionState.Disabled)
        {
            State = DecisionState.Monitoring;
        }

        if (State == DecisionState.ShutdownIssued)
        {
            return Current(ShutdownDecisionAction.None, DecisionTransitionReason.ShutdownIssued);
        }

        if (State == DecisionState.CancelledAwaitingRearm)
        {
            if (idle.InputDetected || idle.IdleDuration <= settings.IdleThreshold)
            {
                State = DecisionState.Monitoring;
                return Current(ShutdownDecisionAction.None, DecisionTransitionReason.ActivityResetRearmed);
            }

            return Current(ShutdownDecisionAction.None, DecisionTransitionReason.RecentInput);
        }

        if (State == DecisionState.Warning)
        {
            return EvaluateWarning(settings, idle, context, now);
        }

        var evaluation = EvaluateEligibility(settings, idle, context, now);
        if (evaluation.IsEligible)
        {
            _warningStartedAt = now;
            State = DecisionState.WarningCountdown;
            return Current(ShutdownDecisionAction.StartWarning, DecisionTransitionReason.WarningStarted);
        }

        State = evaluation.State;
        return Current(ShutdownDecisionAction.None, evaluation.Reason);
    }

    public void CancelAndRequireRearm()
    {
        _warningStartedAt = null;
        State = DecisionState.CancelledAwaitingRearm;
    }

    public void Disable()
    {
        _warningStartedAt = null;
        State = DecisionState.Disabled;
    }

    private DecisionResult EvaluateWarning(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now)
    {
        if (idle.InputDetected)
        {
            CancelAndRequireRearm();
            return Current(ShutdownDecisionAction.CancelWarning, DecisionTransitionReason.WarningCancelledByInput);
        }

        _warningStartedAt ??= now;
        if (now - _warningStartedAt.Value < settings.WarningDuration)
        {
            return Current(ShutdownDecisionAction.None, DecisionTransitionReason.WarningContinuing);
        }

        var evaluation = EvaluateEligibility(settings, idle, context, now);
        if (evaluation.IsEligible)
        {
            State = DecisionState.ShutdownIssued;
            return Current(ShutdownDecisionAction.ShutdownNow, DecisionTransitionReason.ShutdownIssued);
        }

        _warningStartedAt = null;
        State = evaluation.State;
        return Current(ShutdownDecisionAction.CancelWarning, DecisionTransitionReason.WarningCancelledByFinalRecheck);
    }

    private static EligibilityEvaluation EvaluateEligibility(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now)
    {
        if (!MonitoringSchedule.IsInsideEvaluationWindow(settings, now))
        {
            return new EligibilityEvaluation(false, DecisionState.WaitingForWindow, DecisionTransitionReason.WaitingForStartTime);
        }

        if (idle.InputDetected)
        {
            return new EligibilityEvaluation(false, DecisionState.Monitoring, DecisionTransitionReason.RecentInput);
        }

        if (idle.IdleDuration <= settings.IdleThreshold)
        {
            return new EligibilityEvaluation(false, DecisionState.Monitoring, DecisionTransitionReason.IdleThresholdNotMet);
        }

        var blocker = ContextBlockingPolicy.GetEffectiveBlocker(settings, idle, context);
        if (blocker is not null)
        {
            return new EligibilityEvaluation(
                false,
                DecisionState.ShutdownBlocked,
                ContextBlockingPolicy.IsHardBlocker(blocker)
                    ? DecisionTransitionReason.DetectorFailureBlocked
                    : DecisionTransitionReason.SoftContextBlocked);
        }

        return new EligibilityEvaluation(true, DecisionState.IdleCandidate, DecisionTransitionReason.IdleCandidate);
    }

    private DecisionResult Current(ShutdownDecisionAction action, DecisionTransitionReason reason)
    {
        return new DecisionResult(action, State, _warningStartedAt, reason);
    }

    private readonly record struct EligibilityEvaluation(
        bool IsEligible,
        DecisionState State,
        DecisionTransitionReason Reason);
}
