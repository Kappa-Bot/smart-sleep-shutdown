using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.Core.Warnings;

public static class WarningPolicy
{
    public static TimeSpan DefaultFor(NightAction action) => action switch
    {
        NightAction.ShutDown => TimeSpan.FromSeconds(60),
        NightAction.Hibernate => TimeSpan.FromSeconds(45),
        NightAction.Sleep => TimeSpan.FromSeconds(30),
        NightAction.Lock => TimeSpan.FromSeconds(10),
        NightAction.WarnOnly => TimeSpan.Zero,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    public static bool IsValid(NightAction action, TimeSpan duration)
    {
        return RoutineValidation.IsWarningDurationValid(action, duration);
    }
}
