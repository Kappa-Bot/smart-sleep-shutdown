using Hushward.Core.Routines;
using Hushward.Core.Tests.TestSupport;

namespace Hushward.Core.Tests.Routines;

public sealed class RoutineOverlapDetectorTests
{
    [Fact]
    public void Finds_overlapping_enabled_routines_on_same_day()
    {
        var first = TestRoutines.Enabled(earliest: new TimeOnly(1, 0), latest: new TimeOnly(4, 0), days: [DayOfWeek.Thursday]);
        var second = TestRoutines.Enabled(earliest: new TimeOnly(3, 0), latest: new TimeOnly(6, 0), days: [DayOfWeek.Thursday]);

        var conflicts = RoutineOverlapDetector.FindConflicts([first, second], TimeZoneInfo.Utc);

        Assert.Single(conflicts);
    }

    [Fact]
    public void Ignores_disabled_routines()
    {
        var first = TestRoutines.Enabled(earliest: new TimeOnly(1, 0), latest: new TimeOnly(4, 0), days: [DayOfWeek.Thursday]);
        var second = TestRoutines.Enabled(earliest: new TimeOnly(3, 0), latest: new TimeOnly(6, 0), days: [DayOfWeek.Thursday]) with
        {
            Enabled = false
        };

        Assert.Empty(RoutineOverlapDetector.FindConflicts([first, second], TimeZoneInfo.Utc));
    }

    [Fact]
    public void Finds_midnight_crossing_overlap_on_carried_day()
    {
        var mondayNight = TestRoutines.Enabled(earliest: new TimeOnly(23, 0), latest: new TimeOnly(2, 0), days: [DayOfWeek.Monday]);
        var tuesdayEarly = TestRoutines.Enabled(earliest: new TimeOnly(1, 0), latest: new TimeOnly(3, 0), days: [DayOfWeek.Tuesday]);

        var conflicts = RoutineOverlapDetector.FindConflicts([mondayNight, tuesdayEarly], TimeZoneInfo.Utc);

        var conflict = Assert.Single(conflicts);
        conflict.LocalDay.ShouldBe(DayOfWeek.Tuesday);
    }

    [Fact]
    public void Exact_boundary_overlap_matches_inclusive_window_semantics()
    {
        var first = TestRoutines.Enabled(earliest: new TimeOnly(1, 0), latest: new TimeOnly(2, 0), days: [DayOfWeek.Thursday]);
        var second = TestRoutines.Enabled(earliest: new TimeOnly(2, 0), latest: new TimeOnly(3, 0), days: [DayOfWeek.Thursday]);

        var conflict = Assert.Single(RoutineOverlapDetector.FindConflicts([first, second], TimeZoneInfo.Utc));

        conflict.LocalDay.ShouldBe(DayOfWeek.Thursday);
    }
}
