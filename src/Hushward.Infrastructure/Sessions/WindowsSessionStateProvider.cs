using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Infrastructure.Sessions;

public sealed class WindowsSessionStateProvider : ISessionStateProvider
{
    private readonly IWindowsSessionApi _sessionApi;
    private readonly SystemTransitionMonitor _transitionMonitor;
    private readonly Func<DateTimeOffset> _now;
    private WindowsSessionSnapshot? _lastSnapshot;

    public WindowsSessionStateProvider()
        : this(new WindowsSessionApi(), new SystemTransitionMonitor(), () => DateTimeOffset.Now)
    {
    }

    internal WindowsSessionStateProvider(
        IWindowsSessionApi sessionApi,
        SystemTransitionMonitor transitionMonitor,
        Func<DateTimeOffset> now)
    {
        _sessionApi = sessionApi;
        _transitionMonitor = transitionMonitor;
        _now = now;
    }

    public Task<OperationResult<SessionRuntimeState>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var snapshot = _sessionApi.ReadSession();
            if (_lastSnapshot is not null && _lastSnapshot.IsLocked != snapshot.IsLocked)
            {
                _transitionMonitor.RecordLockTransition(_now());
            }

            _lastSnapshot = snapshot;
            var transitions = _transitionMonitor.Snapshot();
            return Task.FromResult(OperationResult<SessionRuntimeState>.Success(new SessionRuntimeState(
                snapshot.IsLocked,
                snapshot.IsRemote,
                transitions.LastLockTransitionAt,
                transitions.LastLockTransitionAt,
                transitions.LastPowerTransitionAt,
                transitions.LastDisplayTopologyTransitionAt)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(OperationResult<SessionRuntimeState>.Failure(
                "session.state.unavailable",
                "Session.StateUnavailable",
                ex.Message));
        }
    }
}
