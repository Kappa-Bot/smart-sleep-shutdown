using Hushward.Core.Actions;
using Hushward.Infrastructure.Power;

namespace Hushward.Infrastructure.Tests.Power;

public sealed class WindowsPowerCapabilitiesProviderTests
{
    [Fact]
    public void Supported_actions_include_only_available_power_actions()
    {
        var provider = new WindowsPowerCapabilitiesProvider(
            new FixedPowerApi(new WindowsPowerCapabilities(SleepSupported: true, HibernateSupported: false)));

        var result = provider.ReadSupportedActions();

        Assert.True(result.IsSuccess);
        Assert.Contains(NightAction.ShutDown, result.Value!);
        Assert.Contains(NightAction.Lock, result.Value!);
        Assert.Contains(NightAction.WarnOnly, result.Value!);
        Assert.Contains(NightAction.Sleep, result.Value!);
        Assert.DoesNotContain(NightAction.Hibernate, result.Value!);
    }

    [Fact]
    public void Capability_probe_failure_returns_typed_failure()
    {
        var provider = new WindowsPowerCapabilitiesProvider(new ThrowingPowerApi());

        var result = provider.ReadSupportedActions();

        Assert.False(result.IsSuccess);
        Assert.Equal("power.capabilities.unavailable", result.Error!.Code);
    }

    private sealed class FixedPowerApi : IWindowsPowerApi
    {
        private readonly WindowsPowerCapabilities _capabilities;

        public FixedPowerApi(WindowsPowerCapabilities capabilities)
        {
            _capabilities = capabilities;
        }

        public WindowsPowerLineStatus ReadLineStatus() => new(null, null, null);

        public WindowsPowerCapabilities ReadCapabilities() => _capabilities;

        public bool SetSuspendState(bool hibernate) => true;
    }

    private sealed class ThrowingPowerApi : IWindowsPowerApi
    {
        public WindowsPowerLineStatus ReadLineStatus() => throw new InvalidOperationException("boom");

        public WindowsPowerCapabilities ReadCapabilities() => throw new InvalidOperationException("boom");

        public bool SetSuspendState(bool hibernate) => throw new InvalidOperationException("boom");
    }
}
