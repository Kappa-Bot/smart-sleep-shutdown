using Hushward.Application.Abstractions;
using Hushward.Application.Coordinators;
using Hushward.Application.Results;
using Hushward.Core.Actions;

namespace Hushward.Application.Tests.Coordinators;

public sealed class ActionCoordinatorTests
{
    [Fact]
    public async Task Two_finalization_requests_execute_action_once()
    {
        var executor = new RecordingNightActionExecutor();
        var coordinator = new ActionCoordinator(executor);

        await Task.WhenAll(
            coordinator.ExecuteOnceAsync(42, NightAction.Hibernate, CancellationToken.None),
            coordinator.ExecuteOnceAsync(42, NightAction.Hibernate, CancellationToken.None));

        Assert.Single(executor.Calls);
    }

    [Fact]
    public async Task Cancelled_request_returns_typed_failure_without_executor_call()
    {
        var executor = new RecordingNightActionExecutor();
        var coordinator = new ActionCoordinator(executor);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await coordinator.ExecuteOnceAsync(42, NightAction.Hibernate, cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal("action.cancelled", result.Error?.Code);
        Assert.Empty(executor.Calls);
    }

    private sealed class RecordingNightActionExecutor : INightActionExecutor
    {
        public List<NightAction> Calls { get; } = [];

        public Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken)
        {
            Calls.Add(action);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }
    }
}
