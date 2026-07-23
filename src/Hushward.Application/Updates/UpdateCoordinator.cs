using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Updates;

public sealed record UpdateSafetyContext(
    bool WarningActive,
    bool ActionPendingOrExecuting,
    bool MigrationPending,
    bool RecoveryRequired,
    bool Degraded)
{
    public bool BlocksInstallation =>
        WarningActive ||
        ActionPendingOrExecuting ||
        MigrationPending ||
        RecoveryRequired ||
        Degraded;
}

public sealed class UpdateCoordinator
{
    private readonly IUpdateService _service;
    private readonly Func<UpdateSafetyContext> _readSafetyContext;

    public UpdateCoordinator(
        IUpdateService service,
        Func<UpdateSafetyContext> readSafetyContext)
    {
        _service = service;
        _readSafetyContext = readSafetyContext;
    }

    public Task<OperationResult<UpdateState>> CheckManuallyAsync(CancellationToken cancellationToken) =>
        _service.CheckAsync(cancellationToken);

    public Task<OperationResult<UpdateState>> CheckOptInAsync(
        bool notificationsEnabled,
        CancellationToken cancellationToken) =>
        notificationsEnabled
            ? _service.CheckAsync(cancellationToken)
            : Task.FromResult(OperationResult<UpdateState>.Failure(
                "update.notifications-disabled",
                "Update.NotificationsDisabled"));

    public Task<OperationResult<Unit>> InstallAsync(CancellationToken cancellationToken)
    {
        if (_readSafetyContext().BlocksInstallation)
        {
            return Task.FromResult(OperationResult<Unit>.Failure(
                "update.unsafe-runtime-state",
                "Update.UnsafeRuntimeState"));
        }

        return _service.InstallAsync(cancellationToken);
    }
}
