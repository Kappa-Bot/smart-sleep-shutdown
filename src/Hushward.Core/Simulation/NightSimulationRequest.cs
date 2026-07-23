using Hushward.Core.Decisions;

namespace Hushward.Core.Simulation;

public sealed record NightSimulationRequest(NightEvaluationInput EvaluationInput)
{
    public static NightSimulationRequest FromEvaluationInput(NightEvaluationInput evaluationInput) => new(evaluationInput);

    public NightEvaluationInput ToEvaluationInput() => EvaluationInput;
}
