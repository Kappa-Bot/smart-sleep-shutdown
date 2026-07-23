namespace Hushward.Core.Routines;

public sealed record RoutineOverlapConflict(
    NightRoutine First,
    NightRoutine Second,
    DayOfWeek LocalDay);

public static class RoutineOverlapDetector
{
    private const int MinutesPerDay = 24 * 60;
    private const int MinutesPerWeek = 7 * MinutesPerDay;

    public static IReadOnlyList<RoutineOverlapConflict> FindConflicts(
        IReadOnlyList<NightRoutine> routines,
        TimeZoneInfo timeZone)
    {
        _ = timeZone;
        var intervals = routines
            .Where(routine => routine.Enabled)
            .SelectMany(Expand)
            .ToArray();
        var conflicts = new List<RoutineOverlapConflict>();

        for (var first = 0; first < intervals.Length; first++)
        {
            for (var second = first + 1; second < intervals.Length; second++)
            {
                var a = intervals[first];
                var b = intervals[second];
                if (a.Routine.Id == b.Routine.Id)
                {
                    continue;
                }

                if (a.StartMinute <= b.EndMinute && b.StartMinute <= a.EndMinute)
                {
                    var overlapStart = Math.Max(a.StartMinute, b.StartMinute);
                    conflicts.Add(new RoutineOverlapConflict(a.Routine, b.Routine, ToLocalDay(overlapStart)));
                }
            }
        }

        return conflicts
            .DistinctBy(conflict => CreateConflictKey(conflict.First.Id, conflict.Second.Id))
            .ToArray();
    }

    private static IEnumerable<RoutineInterval> Expand(NightRoutine routine)
    {
        foreach (var day in routine.Days)
        {
            var start = ((int)day * MinutesPerDay) + ToMinuteOfDay(routine.Window.Earliest);
            var end = ((int)day * MinutesPerDay) + ToMinuteOfDay(routine.Window.Latest);
            if (end <= start)
            {
                end += MinutesPerDay;
            }

            yield return new RoutineInterval(routine, day, start, end);
            yield return new RoutineInterval(routine, day, start - MinutesPerWeek, end - MinutesPerWeek);
            yield return new RoutineInterval(routine, day, start + MinutesPerWeek, end + MinutesPerWeek);
        }
    }

    private static int ToMinuteOfDay(TimeOnly time) => (time.Hour * 60) + time.Minute;

    private static DayOfWeek ToLocalDay(int minute)
    {
        var normalized = ((minute % MinutesPerWeek) + MinutesPerWeek) % MinutesPerWeek;
        return (DayOfWeek)(normalized / MinutesPerDay);
    }

    private static string CreateConflictKey(Guid first, Guid second)
    {
        return first.CompareTo(second) < 0
            ? $"{first:N}:{second:N}"
            : $"{second:N}:{first:N}";
    }

    private sealed record RoutineInterval(
        NightRoutine Routine,
        DayOfWeek Day,
        int StartMinute,
        int EndMinute);
}
