using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Core.Routines;

namespace Hushward.Application.Coordinators;

public sealed class ScheduleSyncCoordinator
{
    private readonly IScheduleSynchronizer _synchronizer;

    public ScheduleSyncCoordinator(IScheduleSynchronizer synchronizer)
    {
        _synchronizer = synchronizer;
    }

    public async Task<OperationResult<ScheduleHealth>> SynchronizeAsync(
        IReadOnlyList<NightRoutine> routines,
        CancellationToken cancellationToken)
    {
        var result = await _synchronizer.SynchronizeAsync(routines, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? result
            : OperationResult<ScheduleHealth>.Success(new ScheduleHealth(false, null, result.Error!.Code));
    }
}
