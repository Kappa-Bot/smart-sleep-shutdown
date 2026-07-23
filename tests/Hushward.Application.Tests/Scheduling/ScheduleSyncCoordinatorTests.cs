using Hushward.Application.Abstractions;
using Hushward.Application.Coordinators;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Scheduling;
using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.Application.Tests.Scheduling;

public sealed class ScheduleSyncCoordinatorTests
{
    [Fact]
    public void Never_wake_produces_no_task()
    {
        var routine = EnabledRoutine() with { WakePolicy = WakePolicy.NeverWake };

        Assert.Null(DesiredWakeSchedule.From(routine, TimeZoneInfo.Utc));
    }

    [Fact]
    public void Wake_schedule_tracks_routine_earliest_time()
    {
        var routine = EnabledRoutine() with
        {
            Window = new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0)),
            WakePolicy = WakePolicy.WakeToEvaluate
        };

        var desired = DesiredWakeSchedule.From(routine, TimeZoneInfo.Utc)!;

        Assert.Equal(new TimeOnly(0, 30), desired.LocalStartTime);
        Assert.Equal("--scheduled-check", desired.Arguments);
    }

    [Fact]
    public void Wake_schedule_wraps_precheck_across_midnight()
    {
        var routine = EnabledRoutine() with
        {
            Window = new NightWindow(new TimeOnly(0, 15), new TimeOnly(4, 0)),
            WakePolicy = WakePolicy.WakeToEvaluate
        };

        var desired = DesiredWakeSchedule.From(routine, TimeZoneInfo.Utc)!;

        Assert.Equal(new TimeOnly(23, 45), desired.LocalStartTime);
        Assert.Equal([DayOfWeek.Wednesday], desired.Days);
    }

    [Fact]
    public async Task Coordinator_converts_sync_failure_to_schedule_health()
    {
        var coordinator = new ScheduleSyncCoordinator(new FailingSynchronizer());

        var result = await coordinator.SynchronizeAsync([EnabledRoutine()], CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsHealthy);
        Assert.Equal("schedule.denied", result.Value.LastErrorCode);
    }

    private static NightRoutine EnabledRoutine() =>
        NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Enabled = true,
            PrimaryAction = NightAction.Hibernate,
            WakePolicy = WakePolicy.WakeToEvaluate,
            Days = [DayOfWeek.Thursday]
        };

    private sealed class FailingSynchronizer : IScheduleSynchronizer
    {
        public Task<OperationResult<ScheduleHealth>> SynchronizeAsync(
            IReadOnlyList<NightRoutine> routines,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<ScheduleHealth>.Failure("schedule.denied", "Schedule.Denied"));
    }
}
