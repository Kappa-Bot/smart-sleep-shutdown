using System.Diagnostics;
using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Scheduling;
using Hushward.Core.Routines;

namespace Hushward.Infrastructure.Scheduling;

public interface IScheduledTaskProcessRunner
{
    Task<OperationResult<Unit>> RunAsync(ScheduledTaskCommand command, CancellationToken cancellationToken);
}

public sealed class WindowsTaskSchedulerSync : IScheduleSynchronizer
{
    private readonly string _executablePath;
    private readonly TimeZoneInfo _timeZone;
    private readonly IScheduledTaskProcessRunner _runner;
    private readonly IWakeTaskHealthReader _healthReader;

    public WindowsTaskSchedulerSync(string executablePath)
        : this(executablePath, TimeZoneInfo.Local, new ScheduledTaskProcessRunner())
    {
    }

    public WindowsTaskSchedulerSync(
        string executablePath,
        TimeZoneInfo timeZone,
        IScheduledTaskProcessRunner runner)
        : this(executablePath, timeZone, runner, new WakeTaskHealthReader(runner))
    {
    }

    public WindowsTaskSchedulerSync(
        string executablePath,
        TimeZoneInfo timeZone,
        IScheduledTaskProcessRunner runner,
        IWakeTaskHealthReader healthReader)
    {
        _executablePath = executablePath;
        _timeZone = timeZone;
        _runner = runner;
        _healthReader = healthReader;
    }

    public async Task<OperationResult<ScheduleHealth>> SynchronizeAsync(
        IReadOnlyList<NightRoutine> routines,
        CancellationToken cancellationToken)
    {
        var schedules = DesiredWakeSchedule.FromRoutines(routines, _timeZone);
        if (schedules.Count == 0)
        {
            var deleteProduct = await _runner.RunAsync(
                TaskSchedulerCommandBuilder.BuildDelete(DesiredWakeSchedule.ProductTaskName),
                cancellationToken).ConfigureAwait(false);
            var deleteLegacy = await _runner.RunAsync(
                TaskSchedulerCommandBuilder.BuildDelete(DesiredWakeSchedule.LegacyTaskName),
                cancellationToken).ConfigureAwait(false);
            var errorCode = deleteProduct.Error?.Code ?? deleteLegacy.Error?.Code;
            return errorCode is null
                ? OperationResult<ScheduleHealth>.Success(new ScheduleHealth(true, null, null))
                : OperationResult<ScheduleHealth>.Success(new ScheduleHealth(false, null, errorCode));
        }

        var register = await _runner.RunAsync(
            TaskSchedulerCommandBuilder.BuildRegister(schedules, _executablePath),
            cancellationToken).ConfigureAwait(false);
        if (!register.IsSuccess)
        {
            return OperationResult<ScheduleHealth>.Success(new ScheduleHealth(false, null, register.Error!.Code));
        }

        var health = await _healthReader.ReadAsync(schedules, _executablePath, cancellationToken).ConfigureAwait(false);
        if (!health.IsSuccess || health.Value is null || !health.Value.IsHealthy)
        {
            return health.IsSuccess
                ? health
                : OperationResult<ScheduleHealth>.Success(new ScheduleHealth(false, null, health.Error!.Code));
        }

        await _runner.RunAsync(
            TaskSchedulerCommandBuilder.BuildDelete(DesiredWakeSchedule.LegacyTaskName),
            cancellationToken).ConfigureAwait(false);

        return health;
    }
}

public sealed class ScheduledTaskProcessRunner : IScheduledTaskProcessRunner
{
    public async Task<OperationResult<Unit>> RunAsync(ScheduledTaskCommand command, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo =
                {
                    FileName = command.FileName,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            foreach (var argument in command.Arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return OperationResult<Unit>.Failure("schedule.process-start-failed", "Schedule.ProcessStartFailed");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0
                ? OperationResult<Unit>.Success(new Unit())
                : OperationResult<Unit>.Failure("schedule.process-failed", "Schedule.ProcessFailed", $"Exit code {process.ExitCode}.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return OperationResult<Unit>.Failure("schedule.process-failed", "Schedule.ProcessFailed", ex.Message);
        }
    }
}
