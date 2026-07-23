using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Core.Routines;

namespace Hushward.Application.Abstractions;

public interface IScheduleSynchronizer
{
    Task<OperationResult<ScheduleHealth>> SynchronizeAsync(
        IReadOnlyList<NightRoutine> routines,
        CancellationToken cancellationToken);
}
