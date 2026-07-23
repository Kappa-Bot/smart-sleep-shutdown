using Hushward.Core.Actions;
using Hushward.Core.Protections;
using Hushward.Core.Warnings;

namespace Hushward.Core.Decisions;

public static class NightPolicyEngine
{
    public static NightDecision Evaluate(NightEvaluationInput input)
    {
        if (!input.Routine.Enabled || input.TonightOverride?.PauseUntilTomorrow == true)
        {
            return NightDecision.Blocked(NightDecisionKind.Disabled, DecisionReasonCode.RoutineDisabled);
        }

        var localNow = TimeZoneInfo.ConvertTime(input.EvaluatedAt, input.TimeZone);
        if (!input.Routine.Days.Contains(localNow.DayOfWeek))
        {
            return NightDecision.Blocked(NightDecisionKind.OutsideSchedule, DecisionReasonCode.DayNotSelected);
        }

        var effectiveWindow = input.TonightOverride?.Earliest is { } overrideEarliest
            ? input.Routine.Window with { Earliest = overrideEarliest }
            : input.Routine.Window;
        if (!effectiveWindow.Contains(input.EvaluatedAt, input.TimeZone))
        {
            return NightDecision.Blocked(NightDecisionKind.OutsideSchedule, DecisionReasonCode.OutsideNightWindow);
        }

        if (input.InstallUpdateOrRecoveryActive)
        {
            return NightDecision.Blocked(NightDecisionKind.Degraded, DecisionReasonCode.OperationInProgress);
        }

        var selectedAction = SelectAuthorizedAction(input, out var alternativeSelected);
        if (!input.SupportedActions.Contains(selectedAction))
        {
            return NightDecision.Blocked(NightDecisionKind.CapabilityBlocked, DecisionReasonCode.ActionUnsupported);
        }

        var hasUnknownEvidence = input.Protections.Critical.Any(signal => signal.State == ObservationState.Unknown);
        if (hasUnknownEvidence && selectedAction != NightAction.WarnOnly)
        {
            return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.RequiredEvidenceUnknown);
        }

        if (input.Protections.Critical.Count > 0 && selectedAction != NightAction.WarnOnly)
        {
            return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.CriticalProtectionActive);
        }

        if (input.Protections.Temporary.Count > 0 && selectedAction != NightAction.WarnOnly)
        {
            return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.TemporaryProtectionActive);
        }

        if (input.WarningState.Kind == WarningStateKind.CancelledAwaitingFreshIdle &&
            input.IdleDuration < input.Routine.MinimumIdle)
        {
            return NightDecision.Blocked(NightDecisionKind.WaitingForIdle, DecisionReasonCode.WarningCancelledByInput);
        }

        if (input.IdleDuration < input.Routine.MinimumIdle)
        {
            return NightDecision.Blocked(NightDecisionKind.WaitingForIdle, DecisionReasonCode.IdleThresholdNotMet);
        }

        if (input.LatestDecisionReached &&
            input.Routine.LatestDecisionPolicy == Routines.LatestDecisionPolicy.WarnAndAbandon)
        {
            return NightDecision.Ready(
                NightAction.WarnOnly,
                DecisionReasonCode.LatestDecisionReached,
                TimeSpan.Zero,
                NightDecisionKind.AbandonedForNight);
        }

        if (input.TonightOverride?.RequireManualConfirmation == true)
        {
            return NightDecision.Blocked(NightDecisionKind.ManualConfirmationRequired, DecisionReasonCode.ManualConfirmationRequired);
        }

        if (input.WarningState.Kind == WarningStateKind.Active && input.UserInputDetected)
        {
            return NightDecision.Blocked(NightDecisionKind.WaitingForIdle, DecisionReasonCode.WarningCancelledByInput);
        }

        if (input.WarningState.Kind == WarningStateKind.Active)
        {
            return NightDecision.Ready(
                selectedAction,
                alternativeSelected ? DecisionReasonCode.AuthorizedAlternativeSelected : DecisionReasonCode.Ready,
                GetWarningDuration(input, selectedAction),
                NightDecisionKind.WarningActive);
        }

        var readyReason = alternativeSelected
            ? DecisionReasonCode.AuthorizedAlternativeSelected
            : DecisionReasonCode.Ready;

        if (input.WarningState.Kind == WarningStateKind.CountdownElapsed)
        {
            return NightDecision.Ready(
                selectedAction,
                readyReason,
                null,
                NightDecisionKind.AuthorizedToExecute);
        }

        if (hasUnknownEvidence && selectedAction == NightAction.WarnOnly)
        {
            return NightDecision.Ready(
                selectedAction,
                DecisionReasonCode.RequiredEvidenceUnknown,
                TimeSpan.Zero,
                NightDecisionKind.Degraded);
        }

        return NightDecision.Ready(selectedAction, readyReason, GetWarningDuration(input, selectedAction));
    }

    private static NightAction SelectAuthorizedAction(NightEvaluationInput input, out bool alternativeSelected)
    {
        var primary = input.TonightOverride?.Action ?? input.Routine.PrimaryAction;
        alternativeSelected = false;

        foreach (var alternative in input.Routine.AuthorizedAlternatives)
        {
            if (alternative.Primary != primary)
            {
                continue;
            }

            if (IsConditionSatisfied(alternative.ConditionCode, input))
            {
                alternativeSelected = true;
                return alternative.Alternative;
            }
        }

        return primary;
    }

    private static bool IsConditionSatisfied(string conditionCode, NightEvaluationInput input)
    {
        return conditionCode switch
        {
            "battery-below-20" => input.BatteryPercent is < 20 && !input.BatteryCharging,
            "latest-decision" => input.LatestDecisionReached &&
                input.Routine.LatestDecisionPolicy == Routines.LatestDecisionPolicy.UseAuthorizedAlternative,
            _ => false
        };
    }

    private static TimeSpan GetWarningDuration(NightEvaluationInput input, NightAction selectedAction)
    {
        return selectedAction == input.Routine.PrimaryAction
            ? input.Routine.WarningDuration
            : WarningPolicy.DefaultFor(selectedAction);
    }
}
