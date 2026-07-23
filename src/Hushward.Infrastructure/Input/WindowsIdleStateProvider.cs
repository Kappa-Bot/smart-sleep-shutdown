using System.Runtime.InteropServices;
using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.Infrastructure.Interop;

namespace Hushward.Infrastructure.Input;

public sealed class WindowsIdleStateProvider : IIdleStateProvider
{
    private readonly IWindowsIdleApi _idleApi;
    private readonly Func<DateTimeOffset> _now;
    private TimeSpan? _lastIdleDuration;

    public WindowsIdleStateProvider()
        : this(new WindowsIdleApi(), () => DateTimeOffset.Now)
    {
    }

    internal WindowsIdleStateProvider(IWindowsIdleApi idleApi, Func<DateTimeOffset> now)
    {
        _idleApi = idleApi;
        _now = now;
    }

    public Task<OperationResult<IdleRuntimeState>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var idleDuration = _idleApi.GetIdleDuration();
            var inputDetected = _lastIdleDuration is not null && idleDuration < _lastIdleDuration.Value;
            _lastIdleDuration = idleDuration;
            return Task.FromResult(OperationResult<IdleRuntimeState>.Success(new IdleRuntimeState(
                idleDuration,
                inputDetected,
                _now())));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(OperationResult<IdleRuntimeState>.Failure(
                "input.idle.unavailable",
                "Input.IdleUnavailable",
                ex.Message));
        }
    }
}

internal interface IWindowsIdleApi
{
    TimeSpan GetIdleDuration();
}

internal sealed class WindowsIdleApi : IWindowsIdleApi
{
    public TimeSpan GetIdleDuration()
    {
        var info = new NativeMethods.LastInputInfo
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.LastInputInfo>()
        };

        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            throw new InvalidOperationException("GetLastInputInfo failed.");
        }

        var currentTick = unchecked((uint)NativeMethods.GetTickCount64());
        var idleMilliseconds = unchecked(currentTick - info.DwTime);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }
}
