using Hushward.Core.Actions;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.Core.Tests.TestSupport;

public static class TestRoutines
{
    public static NightRoutine Enabled(
        NightAction action = NightAction.Hibernate,
        TimeOnly? earliest = null,
        TimeOnly? latest = null,
        params DayOfWeek[] days) =>
        NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Enabled = true,
            PrimaryAction = action,
            WarningDuration = WarningPolicy.DefaultFor(action),
            Window = new NightWindow(earliest ?? new TimeOnly(1, 0), latest ?? new TimeOnly(6, 0)),
            Days = days.Length == 0 ? [DayOfWeek.Thursday] : days
        };
}
