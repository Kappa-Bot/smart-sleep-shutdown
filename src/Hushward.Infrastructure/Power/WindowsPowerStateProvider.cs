using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Infrastructure.Power;

public sealed class WindowsPowerStateProvider : IPowerStateProvider
{
    private readonly IWindowsPowerApi _powerApi;
    private readonly WindowsPowerCapabilitiesProvider _capabilitiesProvider;
    private readonly Func<DateTimeOffset> _now;
    private WindowsPowerLineStatus? _lastStatus;
    private DateTimeOffset? _lastTransitionAt;

    public WindowsPowerStateProvider()
        : this(new WindowsPowerApi(), () => DateTimeOffset.Now)
    {
    }

    internal WindowsPowerStateProvider(IWindowsPowerApi powerApi, Func<DateTimeOffset> now)
    {
        _powerApi = powerApi;
        _capabilitiesProvider = new WindowsPowerCapabilitiesProvider(powerApi);
        _now = now;
    }

    public Task<OperationResult<PowerRuntimeState>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var status = _powerApi.ReadLineStatus();
            if (_lastStatus is not null && _lastStatus != status)
            {
                _lastTransitionAt = _now();
            }

            _lastStatus = status;
            var capabilities = _capabilitiesProvider.ReadSupportedActions();
            if (!capabilities.IsSuccess)
            {
                return Task.FromResult(OperationResult<PowerRuntimeState>.Failure(
                    capabilities.Error!.Code,
                    capabilities.Error.MessageKey,
                    capabilities.Error.TechnicalDetail));
            }

            return Task.FromResult(OperationResult<PowerRuntimeState>.Success(new PowerRuntimeState(
                status.IsOnBattery,
                status.BatteryPercent,
                status.IsCharging,
                capabilities.Value!,
                _lastTransitionAt)));
        }
        catch (Exception ex) when (ex is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return Task.FromResult(OperationResult<PowerRuntimeState>.Failure(
                "power.state.unavailable",
                "Power.StateUnavailable",
                ex.Message));
        }
    }
}
