using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Velopack;

namespace Hushward.Infrastructure.Updates;

internal interface IVelopackClient
{
    bool IsInstalled { get; }

    Task<object?> CheckAsync(CancellationToken cancellationToken);

    Task DownloadAsync(object update, CancellationToken cancellationToken);

    void ApplyAndRestart(object update);
}

internal sealed class VelopackClient : IVelopackClient
{
    private readonly UpdateManager _manager;

    public VelopackClient(string releaseFeed)
    {
        _manager = new UpdateManager(releaseFeed);
    }

    public bool IsInstalled => _manager.IsInstalled;

    public async Task<object?> CheckAsync(CancellationToken cancellationToken) =>
        await _manager.CheckForUpdatesAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

    public Task DownloadAsync(object update, CancellationToken cancellationToken) =>
        _manager.DownloadUpdatesAsync((UpdateInfo)update, progress: null, cancellationToken);

    public void ApplyAndRestart(object update) =>
        _manager.ApplyUpdatesAndRestart(((UpdateInfo)update).TargetFullRelease, []);
}

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly IVelopackClient _client;
    private object? _pendingUpdate;

    public VelopackUpdateService(string releaseFeed)
        : this(new VelopackClient(releaseFeed))
    {
    }

    internal VelopackUpdateService(IVelopackClient client)
    {
        _client = client;
    }

    public async Task<OperationResult<UpdateState>> CheckAsync(CancellationToken cancellationToken)
    {
        if (!_client.IsInstalled)
        {
            return OperationResult<UpdateState>.Failure(
                "update.not-installed",
                "Update.NotInstalled");
        }

        try
        {
            _pendingUpdate = await _client.CheckAsync(cancellationToken).ConfigureAwait(false);
            return OperationResult<UpdateState>.Success(new UpdateState(
                IsChecking: false,
                UpdateAvailable: _pendingUpdate is not null,
                LastErrorCode: null));
        }
        catch (OperationCanceledException)
        {
            return OperationResult<UpdateState>.Failure("update.cancelled", "Update.Cancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or HttpRequestException)
        {
            return OperationResult<UpdateState>.Failure(
                "update.check-failed",
                "Update.CheckFailed",
                ex.Message);
        }
    }

    public async Task<OperationResult<Unit>> InstallAsync(CancellationToken cancellationToken)
    {
        if (_pendingUpdate is null)
        {
            return OperationResult<Unit>.Failure(
                "update.not-checked",
                "Update.NotChecked");
        }

        try
        {
            await _client.DownloadAsync(_pendingUpdate, cancellationToken).ConfigureAwait(false);
            _client.ApplyAndRestart(_pendingUpdate);
            return OperationResult<Unit>.Success(new Unit());
        }
        catch (OperationCanceledException)
        {
            return OperationResult<Unit>.Failure("update.cancelled", "Update.Cancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or HttpRequestException)
        {
            return OperationResult<Unit>.Failure(
                "update.install-failed",
                "Update.InstallFailed",
                ex.Message);
        }
    }
}
