using Hushward.Core.Actions;

namespace Hushward.Core.Decisions;

public sealed record NightDecision(
    NightDecisionKind Kind,
    NightAction? AuthorizedAction,
    DecisionReasonCode PrimaryReason,
    IReadOnlyList<DecisionReasonCode> SupportingReasons,
    TimeSpan? WarningDuration,
    DateTimeOffset? NextEvaluationAt)
{
    public static NightDecision Blocked(
        NightDecisionKind kind,
        DecisionReasonCode reason,
        params DecisionReasonCode[] supportingReasons) => new(
            kind,
            null,
            reason,
            Array.AsReadOnly(supportingReasons.ToArray()),
            null,
            null);

    public static NightDecision Ready(
        NightAction action,
        DecisionReasonCode reason,
        TimeSpan? warningDuration,
        NightDecisionKind kind = NightDecisionKind.ReadyToWarn,
        params DecisionReasonCode[] supportingReasons) => new(
            kind,
            action,
            reason,
            Array.AsReadOnly(supportingReasons.ToArray()),
            warningDuration,
            null);
}
