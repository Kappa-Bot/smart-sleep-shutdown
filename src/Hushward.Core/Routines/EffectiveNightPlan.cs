using Hushward.Core.Actions;

namespace Hushward.Core.Routines;

public sealed record EffectiveNightPlan(
    Guid RoutineId,
    bool Enabled,
    NightAction Action,
    NightWindow Window,
    TimeSpan MinimumIdle,
    TimeSpan WarningDuration,
    WakePolicy WakePolicy,
    bool RequireManualConfirmation,
    NightRoutine Routine,
    TonightOverride? ActiveOverride);
