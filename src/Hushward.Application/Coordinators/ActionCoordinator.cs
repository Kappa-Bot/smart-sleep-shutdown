using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Actions;

namespace Hushward.Application.Coordinators;

public sealed class ActionCoordinator
{
    private readonly INightActionExecutor _executor;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<long, NightAction> _authorizedSequences = [];

    public ActionCoordinator(INightActionExecutor executor)
    {
        _executor = executor;
    }

    public async Task<OperationResult<Unit>> ExecuteOnceAsync(
        long authorizedSequence,
        NightAction action,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<Unit>.Failure("action.cancelled", "Action.Cancelled");
        }

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<Unit>.Failure("action.cancelled", "Action.Cancelled");
        }

        try
        {
            if (_authorizedSequences.TryGetValue(authorizedSequence, out var existingAction))
            {
                return existingAction == action
                    ? OperationResult<Unit>.Failure("action.duplicate", "Action.Duplicate")
                    : OperationResult<Unit>.Failure("action.mismatch", "Action.Mismatch");
            }

            _authorizedSequences.Add(authorizedSequence, action);
        }
        finally
        {
            _gate.Release();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<Unit>.Failure("action.cancelled", "Action.Cancelled");
        }

        return await _executor.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
    }
}
