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
            return Current(ShutdownDecisionAction.None);
        }

        if (State == DecisionState.Disabled)
        {
            State = DecisionState.Monitoring;
        }

        if (State == DecisionState.ShutdownIssued)
        {
            return Current(ShutdownDecisionAction.None);
        }

        if (State == DecisionState.CancelledAwaitingRearm)
        {
            if (idle.InputDetected || idle.IdleDuration <= settings.IdleThreshold)
            {
                State = DecisionState.Monitoring;
            }

            return Current(ShutdownDecisionAction.None);
        }

        if (State == DecisionState.Warning)
        {
            return EvaluateWarning(settings, idle, context, now);
        }

        if (IsEligible(settings, idle, context, now))
        {
            _warningStartedAt = now;
            State = DecisionState.Warning;
            return Current(ShutdownDecisionAction.StartWarning);
        }

        State = DecisionState.Monitoring;
        return Current(ShutdownDecisionAction.None);
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
            return Current(ShutdownDecisionAction.CancelWarning);
        }

        _warningStartedAt ??= now;
        if (now - _warningStartedAt.Value < settings.WarningDuration)
        {
            return Current(ShutdownDecisionAction.None);
        }

        if (IsEligible(settings, idle, context, now))
        {
            State = DecisionState.ShutdownIssued;
            return Current(ShutdownDecisionAction.ShutdownNow);
        }

        _warningStartedAt = null;
        State = DecisionState.Monitoring;
        return Current(ShutdownDecisionAction.CancelWarning);
    }

    private static bool IsEligible(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now)
    {
        return MonitoringSchedule.IsInsideEvaluationWindow(settings, now)
            && idle.IdleDuration > settings.IdleThreshold
            && !idle.InputDetected
            && !ContextBlockingPolicy.BlocksShutdown(settings, idle, context);
    }

    private DecisionResult Current(ShutdownDecisionAction action)
    {
        return new DecisionResult(action, State, _warningStartedAt);
    }
}
