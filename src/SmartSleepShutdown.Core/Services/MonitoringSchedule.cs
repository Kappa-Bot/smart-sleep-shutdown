using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Core.Services;

public static class MonitoringSchedule
{
    public static TimeOnly PrecheckTime { get; } = new(0, 30);

    public static TimeSpan PrecheckLeadTime { get; } = TimeSpan.FromMinutes(30);

    public static TimeOnly StopCheckingTime { get; } = new(6, 0);

    public static bool IsInsideEvaluationWindow(SleepShutdownSettings settings, DateTimeOffset now)
    {
        var currentStart = GetCurrentStart(settings, now);
        var currentStop = StopForStart(currentStart);
        return now >= currentStart && now < currentStop;
    }

    public static TimeSpan GetDelayBeforeNextEvaluation(SleepShutdownSettings settings, DateTimeOffset now)
    {
        if (!settings.Enabled)
        {
            return TimeSpan.FromMinutes(5);
        }

        if (IsInsideEvaluationWindow(settings, now))
        {
            return TimeSpan.Zero;
        }

        var nextStart = DateTimeOffsetFor(now, settings.StartTime);
        if (nextStart <= now)
        {
            nextStart = nextStart.AddDays(1);
        }

        var nextPrecheck = nextStart - PrecheckLeadTime;
        if (now >= nextPrecheck)
        {
            return nextStart - now;
        }

        return nextPrecheck - now;
    }

    public static TimeSpan GetDelayAfterEvaluation(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        DecisionState state)
    {
        if (state == DecisionState.Warning)
        {
            return TimeSpan.FromSeconds(1);
        }

        var remainingIdle = settings.IdleThreshold - idle.IdleDuration;
        return remainingIdle <= TimeSpan.FromMinutes(2)
            ? TimeSpan.FromSeconds(5)
            : TimeSpan.FromMinutes(1);
    }

    private static DateTimeOffset DateTimeOffsetFor(DateTimeOffset source, TimeOnly time)
    {
        var local = source.Date + time.ToTimeSpan();
        return new DateTimeOffset(local, source.Offset);
    }

    private static DateTimeOffset GetCurrentStart(SleepShutdownSettings settings, DateTimeOffset now)
    {
        var currentStart = DateTimeOffsetFor(now, settings.StartTime);
        return currentStart > now ? currentStart.AddDays(-1) : currentStart;
    }

    private static DateTimeOffset StopForStart(DateTimeOffset start)
    {
        var stop = new DateTimeOffset(start.Date + StopCheckingTime.ToTimeSpan(), start.Offset);
        return stop <= start ? stop.AddDays(1) : stop;
    }
}
