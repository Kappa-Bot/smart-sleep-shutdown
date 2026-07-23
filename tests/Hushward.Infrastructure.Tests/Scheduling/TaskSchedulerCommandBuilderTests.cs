using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Scheduling;
using Hushward.Core.Routines;
using Hushward.Infrastructure.Scheduling;

namespace Hushward.Infrastructure.Tests.Scheduling;

public sealed class TaskSchedulerCommandBuilderTests
{
    [Fact]
    public void Register_command_uses_wake_to_run_repetition_and_scheduled_check()
    {
        var schedule = new DesiredWakeSchedule(
            DesiredWakeSchedule.ProductTaskName,
            new TimeOnly(0, 30),
            [DayOfWeek.Thursday],
            DesiredWakeSchedule.ScheduledCheckArgument,
            WakePolicy.WakeToEvaluate);

        var command = TaskSchedulerCommandBuilder.BuildRegister([schedule], @"C:\Users\me\AppData\Local\Hushward\Hushward.App.exe");

        var script = string.Join(" ", command.Arguments);
        Assert.Equal("powershell.exe", command.FileName);
        Assert.Contains("Hushward-NightWake", script, StringComparison.Ordinal);
        Assert.Contains("-WakeToRun", script, StringComparison.Ordinal);
        Assert.Contains("--scheduled-check", script, StringComparison.Ordinal);
        Assert.Contains("New-ScheduledTaskRepetitionSettings", script, StringComparison.Ordinal);
        Assert.Contains("00:30", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_check_verifies_action_trigger_wake_and_repetition()
    {
        var schedule = new DesiredWakeSchedule(
            DesiredWakeSchedule.ProductTaskName,
            new TimeOnly(23, 45),
            [DayOfWeek.Wednesday],
            DesiredWakeSchedule.ScheduledCheckArgument,
            WakePolicy.WakeToEvaluate);

        var command = TaskSchedulerCommandBuilder.BuildHealthCheck([schedule], @"C:\Hushward\Hushward.App.exe");

        var script = CommandText(command);
        Assert.Contains("Get-ScheduledTask", script, StringComparison.Ordinal);
        Assert.Contains("--scheduled-check", script, StringComparison.Ordinal);
        Assert.Contains("WakeToRun", script, StringComparison.Ordinal);
        Assert.Contains("23:45", script, StringComparison.Ordinal);
        Assert.Contains("Wednesday", script, StringComparison.Ordinal);
        Assert.Contains("$triggers.Count -ne 1", script, StringComparison.Ordinal);
        Assert.Contains("PT5M", script, StringComparison.Ordinal);
        Assert.Contains("PT6H", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_task_is_removed_only_after_new_task_reads_back_healthy()
    {
        var runner = new RecordingRunner();
        var sync = new WindowsTaskSchedulerSync(
            @"C:\Hushward\Hushward.App.exe",
            TimeZoneInfo.Utc,
            runner,
            new FixedHealthReader(new ScheduleHealth(true, new DateTimeOffset(2026, 7, 24, 0, 30, 0, TimeSpan.Zero), null)));

        var result = await sync.SynchronizeAsync([EnabledRoutine()], CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsHealthy);
        Assert.Contains(runner.Commands, command => CommandText(command).Contains("Register-ScheduledTask", StringComparison.Ordinal));
        Assert.EndsWith("SmartSleepShutdown-NightWake' -Confirm:$false -ErrorAction SilentlyContinue", CommandText(runner.Commands[^1]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_task_is_preserved_when_new_task_health_fails()
    {
        var runner = new RecordingRunner();
        var sync = new WindowsTaskSchedulerSync(
            @"C:\Hushward\Hushward.App.exe",
            TimeZoneInfo.Utc,
            runner,
            new FixedHealthReader(new ScheduleHealth(false, null, "schedule.not-found")));

        var result = await sync.SynchronizeAsync([EnabledRoutine()], CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsHealthy);
        Assert.DoesNotContain(runner.Commands, command => CommandText(command).Contains("SmartSleepShutdown-NightWake", StringComparison.Ordinal));
    }

    [Fact]
    public async Task No_wake_schedule_unregisters_product_and_legacy_tasks()
    {
        var runner = new RecordingRunner();
        var sync = new WindowsTaskSchedulerSync(
            @"C:\Hushward\Hushward.App.exe",
            TimeZoneInfo.Utc,
            runner,
            new FixedHealthReader(new ScheduleHealth(true, null, null)));

        var result = await sync.SynchronizeAsync([EnabledRoutine() with { WakePolicy = WakePolicy.NeverWake }], CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, runner.Commands.Count);
        Assert.Contains("Hushward-NightWake", CommandText(runner.Commands[0]), StringComparison.Ordinal);
        Assert.DoesNotContain("SmartSleepShutdown-NightWake", CommandText(runner.Commands[0]), StringComparison.Ordinal);
        Assert.Contains("SmartSleepShutdown-NightWake", CommandText(runner.Commands[1]), StringComparison.Ordinal);
    }

    private static NightRoutine EnabledRoutine() =>
        NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Enabled = true,
            WakePolicy = WakePolicy.WakeToEvaluate,
            Days = [DayOfWeek.Thursday]
        };

    private static string CommandText(ScheduledTaskCommand command) => string.Join(" ", command.Arguments);

    private sealed class RecordingRunner : IScheduledTaskProcessRunner
    {
        public List<ScheduledTaskCommand> Commands { get; } = [];

        public Task<OperationResult<Unit>> RunAsync(ScheduledTaskCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }
    }

    private sealed class FixedHealthReader : IWakeTaskHealthReader
    {
        private readonly ScheduleHealth _health;

        public FixedHealthReader(ScheduleHealth health)
        {
            _health = health;
        }

        public Task<OperationResult<ScheduleHealth>> ReadAsync(
            IReadOnlyList<DesiredWakeSchedule> schedules,
            string executablePath,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<ScheduleHealth>.Success(_health));
    }
}
