using Hushward.Core.Routines;

namespace Hushward.Application.Coordinators;

public sealed class TonightOverrideCoordinator
{
    public EffectiveNightPlan Resolve(
        NightRoutine routine,
        TonightOverride? tonightOverride,
        DateTimeOffset now) =>
        TonightOverrideResolver.Resolve(routine, tonightOverride, now);
}
