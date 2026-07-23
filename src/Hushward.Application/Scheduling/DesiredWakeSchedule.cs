using Hushward.Core.Routines;

namespace Hushward.Application.Scheduling;

public sealed record DesiredWakeSchedule(
    string TaskName,
    TimeOnly LocalStartTime,
    IReadOnlyList<DayOfWeek> Days,
    string Arguments,
    WakePolicy WakePolicy)
{
    public const string ProductTaskName = "Hushward-NightWake";
    public const string LegacyTaskName = "SmartSleepShutdown-NightWake";
    public const string ScheduledCheckArgument = "--scheduled-check";
    public static readonly TimeSpan PrecheckLeadTime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan RepetitionInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RepetitionDuration = TimeSpan.FromHours(6);

    private IReadOnlyList<DayOfWeek> _days = Array.AsReadOnly(Days.ToArray());

    public IReadOnlyList<DayOfWeek> Days
    {
        get => _days;
        init => _days = Array.AsReadOnly(value.ToArray());
    }

    public static DesiredWakeSchedule? From(NightRoutine routine, TimeZoneInfo timeZone)
    {
        _ = timeZone;
        if (!routine.Enabled || routine.WakePolicy == WakePolicy.NeverWake)
        {
            return null;
        }

        var start = routine.Window.Earliest.ToTimeSpan() - PrecheckLeadTime;
        var wrapsToPreviousDay = start < TimeSpan.Zero;
        if (wrapsToPreviousDay)
        {
            start += TimeSpan.FromDays(1);
        }

        return new DesiredWakeSchedule(
            ProductTaskName,
            TimeOnly.FromTimeSpan(start),
            wrapsToPreviousDay ? routine.Days.Select(PreviousDay).ToArray() : routine.Days,
            ScheduledCheckArgument,
            routine.WakePolicy);
    }

    public static IReadOnlyList<DesiredWakeSchedule> FromRoutines(
        IReadOnlyList<NightRoutine> routines,
        TimeZoneInfo timeZone) =>
        routines
            .Select(routine => From(routine, timeZone))
            .OfType<DesiredWakeSchedule>()
            .OrderBy(schedule => schedule.LocalStartTime)
            .ThenBy(schedule => string.Join(",", schedule.Days.Order()))
            .ToArray();

    private static DayOfWeek PreviousDay(DayOfWeek day) =>
        day == DayOfWeek.Sunday ? DayOfWeek.Saturday : day - 1;
}
