using Hushward.Core.Actions;
using Hushward.Core.Decisions;

namespace Hushward.Core.Simulation;

public sealed record NightSimulationResult(
    NightDecision Decision,
    NightAction? AuthorizedAction,
    DecisionReasonCode PrimaryReason,
    IReadOnlyList<DecisionReasonCode> SupportingReasons,
    DateTimeOffset? NextEvaluationAt,
    bool WouldBeginWarning);
