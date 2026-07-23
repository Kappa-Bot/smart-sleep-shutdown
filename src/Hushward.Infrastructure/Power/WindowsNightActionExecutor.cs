using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Actions;
using Hushward.Infrastructure.Sessions;

namespace Hushward.Infrastructure.Power;

public sealed class WindowsNightActionExecutor : INightActionExecutor
{
    private readonly IProcessLauncher _processLauncher;
    private readonly IWindowsPowerApi _powerApi;
    private readonly IWindowsSessionApi _sessionApi;

    public WindowsNightActionExecutor()
        : this(new ProcessLauncher(), new WindowsPowerApi(), new WindowsSessionApi())
    {
    }

    internal WindowsNightActionExecutor(
        IProcessLauncher processLauncher,
        IWindowsPowerApi powerApi,
        IWindowsSessionApi sessionApi)
    {
        _processLauncher = processLauncher;
        _powerApi = powerApi;
        _sessionApi = sessionApi;
    }

    public async Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return action switch
            {
                NightAction.ShutDown => await ShutDownAsync(cancellationToken).ConfigureAwait(false),
                NightAction.Hibernate => ExecuteSuspend(hibernate: true),
                NightAction.Sleep => ExecuteSuspend(hibernate: false),
                NightAction.Lock => ExecuteLock(),
                NightAction.WarnOnly => OperationResult<Unit>.Success(new Unit()),
                _ => OperationResult<Unit>.Failure("action.unsupported", "Action.Unsupported")
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return OperationResult<Unit>.Failure("action.execution-failed", "Action.ExecutionFailed", ex.Message);
        }
    }

    private async Task<OperationResult<Unit>> ShutDownAsync(CancellationToken cancellationToken)
    {
        var exitCode = await _processLauncher.LaunchAsync(
            new ProcessLaunchRequest(
                "shutdown.exe",
                ["/s", "/t", "0"],
                UseShellExecute: false,
                CreateNoWindow: true),
            cancellationToken).ConfigureAwait(false);

        return exitCode == 0
            ? OperationResult<Unit>.Success(new Unit())
            : OperationResult<Unit>.Failure("power.shutdown.failed", "Power.ShutdownFailed", $"Exit code {exitCode}.");
    }

    private OperationResult<Unit> ExecuteSuspend(bool hibernate)
    {
        var capabilities = _powerApi.ReadCapabilities();
        if (hibernate && !capabilities.HibernateSupported)
        {
            return OperationResult<Unit>.Failure("power.hibernate.unsupported", "Power.HibernateUnsupported");
        }

        if (!hibernate && !capabilities.SleepSupported)
        {
            return OperationResult<Unit>.Failure("power.sleep.unsupported", "Power.SleepUnsupported");
        }

        return _powerApi.SetSuspendState(hibernate)
            ? OperationResult<Unit>.Success(new Unit())
            : OperationResult<Unit>.Failure(
                hibernate ? "power.hibernate.failed" : "power.sleep.failed",
                hibernate ? "Power.HibernateFailed" : "Power.SleepFailed");
    }

    private OperationResult<Unit> ExecuteLock() =>
        _sessionApi.LockWorkStation()
            ? OperationResult<Unit>.Success(new Unit())
            : OperationResult<Unit>.Failure("session.lock.failed", "Session.LockFailed");
}
