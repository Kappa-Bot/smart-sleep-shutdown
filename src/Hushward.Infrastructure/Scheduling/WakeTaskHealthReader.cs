using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Scheduling;

namespace Hushward.Infrastructure.Scheduling;

public interface IWakeTaskHealthReader
{
    Task<OperationResult<ScheduleHealth>> ReadAsync(
        IReadOnlyList<DesiredWakeSchedule> schedules,
        string executablePath,
        CancellationToken cancellationToken);
}

public sealed class WakeTaskHealthReader : IWakeTaskHealthReader
{
    private readonly IScheduledTaskProcessRunner _runner;

    public WakeTaskHealthReader(IScheduledTaskProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<OperationResult<ScheduleHealth>> ReadAsync(
        IReadOnlyList<DesiredWakeSchedule> schedules,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(
            TaskSchedulerCommandBuilder.BuildHealthCheck(schedules, executablePath),
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? OperationResult<ScheduleHealth>.Success(new ScheduleHealth(true, null, null))
            : OperationResult<ScheduleHealth>.Success(new ScheduleHealth(false, null, result.Error!.Code));
    }
}
