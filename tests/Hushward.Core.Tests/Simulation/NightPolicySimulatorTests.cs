using Hushward.Core.Decisions;
using Hushward.Core.Simulation;
using Hushward.Core.Tests.TestSupport;

namespace Hushward.Core.Tests.Simulation;

public sealed class NightPolicySimulatorTests
{
    [Fact]
    public void Simulation_matches_direct_policy_evaluation()
    {
        var request = NightSimulationRequest.FromEvaluationInput(NightEvaluationInputBuilder.Eligible());
        var simulated = NightPolicySimulator.Simulate(request);
        var direct = NightPolicyEngine.Evaluate(request.ToEvaluationInput());

        Assert.Equal(direct, simulated.Decision);
        simulated.WouldBeginWarning.ShouldBe(direct.Kind == NightDecisionKind.ReadyToWarn);
    }
}
