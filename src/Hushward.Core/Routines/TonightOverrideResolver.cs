using Hushward.Core.Warnings;

namespace Hushward.Core.Routines;

public static class TonightOverrideResolver
{
    public static EffectiveNightPlan Resolve(
        NightRoutine routine,
        TonightOverride? tonightOverride,
        DateTimeOffset now)
    {
        var activeOverride = IsActiveForRoutine(routine, tonightOverride, now)
            ? tonightOverride
            : null;
        var action = activeOverride?.Action ?? routine.PrimaryAction;
        var window = routine.Window;

        if (activeOverride?.Earliest is { } earliest)
        {
            window = window with { Earliest = earliest };
        }

        if (activeOverride?.PostponedUntil is { } postponedUntil)
        {
            window = window with { Earliest = TimeOnly.FromDateTime(postponedUntil.DateTime) };
        }

        return new EffectiveNightPlan(
            routine.Id,
            routine.Enabled && activeOverride?.PauseUntilTomorrow != true,
            action,
            window,
            routine.MinimumIdle,
            action == routine.PrimaryAction ? routine.WarningDuration : WarningPolicy.DefaultFor(action),
            activeOverride?.DisableWake == true ? WakePolicy.NeverWake : routine.WakePolicy,
            activeOverride?.RequireManualConfirmation == true,
            routine,
            activeOverride);
    }

    private static bool IsActiveForRoutine(
        NightRoutine routine,
        TonightOverride? tonightOverride,
        DateTimeOffset now)
    {
        return tonightOverride is not null &&
            tonightOverride.RoutineId == routine.Id &&
            tonightOverride.ExpiresAt > now;
    }
}
