using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Actions;
using Hushward.Infrastructure.Power;

namespace Hushward.Infrastructure.Tests.Power;

public sealed class WindowsShutdownExecutorTests
{
    [Fact]
    public async Task Legacy_shutdown_executor_delegates_to_typed_night_action_executor()
    {
        var actionExecutor = new RecordingNightActionExecutor(OperationResult<Unit>.Success(new Unit()));
        var executor = new WindowsShutdownExecutor(actionExecutor);

        await executor.ShutdownNowAsync(CancellationToken.None);

        Assert.Equal([NightAction.ShutDown], actionExecutor.Actions);
    }

    [Fact]
    public async Task Legacy_shutdown_executor_maps_typed_failure_without_starting_own_process()
    {
        var actionExecutor = new RecordingNightActionExecutor(
            OperationResult<Unit>.Failure("power.shutdown.failed", "Power.ShutdownFailed"));
        var executor = new WindowsShutdownExecutor(actionExecutor);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ShutdownNowAsync(CancellationToken.None));

        Assert.Contains("power.shutdown.failed", ex.Message, StringComparison.Ordinal);
        Assert.Equal([NightAction.ShutDown], actionExecutor.Actions);
    }

    private sealed class RecordingNightActionExecutor : INightActionExecutor
    {
        private readonly OperationResult<Unit> _result;

        public RecordingNightActionExecutor(OperationResult<Unit> result)
        {
            _result = result;
        }

        public List<NightAction> Actions { get; } = [];

        public Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken)
        {
            Actions.Add(action);
            return Task.FromResult(_result);
        }
    }
}
