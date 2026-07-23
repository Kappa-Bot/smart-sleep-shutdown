using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Core.Services;

public static class ContextBlockingPolicy
{
    public static TimeSpan SoftBlockerOverrideIdleThreshold { get; } = TimeSpan.FromHours(1);

    public static bool BlocksShutdown(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context)
    {
        return GetEffectiveBlocker(settings, idle, context) is not null;
    }

    public static BlockingContext? GetEffectiveBlocker(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context)
    {
        if (!context.HasBlockingContext)
        {
            return null;
        }

        var hardBlocker = context.Blockers.FirstOrDefault(IsHardBlocker);
        if (hardBlocker is not null)
        {
            return hardBlocker;
        }

        if (!settings.ContextChecksEnabled)
        {
            return null;
        }

        return idle.IdleDuration < SoftBlockerOverrideIdleThreshold
            ? context.Blockers.FirstOrDefault()
            : null;
    }

    public static bool IsHardBlocker(BlockingContext blocker)
    {
        return blocker.Severity == BlockingContextSeverity.Hard
            || blocker.Type == BlockingContextType.DetectorFailure;
    }
}
