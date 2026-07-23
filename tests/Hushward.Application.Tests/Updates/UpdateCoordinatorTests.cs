using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Application.Updates;

namespace Hushward.Application.Tests.Updates;

public sealed class UpdateCoordinatorTests
{
    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public async Task UnsafeRuntimeStateBlocksUpdateInstall(
        bool warning,
        bool action,
        bool migration,
        bool recovery)
    {
        var service = new RecordingUpdateService();
        var coordinator = new UpdateCoordinator(
            service,
            () => new UpdateSafetyContext(warning, action, migration, recovery, Degraded: false));

        var result = await coordinator.InstallAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, service.InstallCalls);
    }

    private sealed class RecordingUpdateService : IUpdateService
    {
        public int InstallCalls { get; private set; }

        public Task<OperationResult<UpdateState>> CheckAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<UpdateState>.Success(new UpdateState(false, true, null)));

        public Task<OperationResult<Unit>> InstallAsync(CancellationToken cancellationToken)
        {
            InstallCalls++;
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }
    }
}
