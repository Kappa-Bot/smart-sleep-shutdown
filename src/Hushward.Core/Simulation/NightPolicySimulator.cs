using Hushward.Core.Decisions;

namespace Hushward.Core.Simulation;

public static class NightPolicySimulator
{
    public static NightSimulationResult Simulate(NightSimulationRequest request)
    {
        var decision = NightPolicyEngine.Evaluate(request.ToEvaluationInput());
        return new NightSimulationResult(
            decision,
            decision.AuthorizedAction,
            decision.PrimaryReason,
            decision.SupportingReasons,
            decision.NextEvaluationAt,
            decision.Kind == NightDecisionKind.ReadyToWarn);
    }
}
