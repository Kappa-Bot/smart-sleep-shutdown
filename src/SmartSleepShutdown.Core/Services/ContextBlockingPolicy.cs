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
        if (!settings.ContextChecksEnabled || !context.HasBlockingContext)
        {
            return false;
        }

        if (context.Blockers.Any(static blocker => blocker.Type == BlockingContextType.DetectorFailure))
        {
            return true;
        }

        return idle.IdleDuration < SoftBlockerOverrideIdleThreshold;
    }
}
