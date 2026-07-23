using Hushward.Application.Results;
using Hushward.Core.Actions;

namespace Hushward.Infrastructure.Power;

public sealed class WindowsPowerCapabilitiesProvider
{
    private readonly IWindowsPowerApi _powerApi;

    public WindowsPowerCapabilitiesProvider()
        : this(new WindowsPowerApi())
    {
    }

    internal WindowsPowerCapabilitiesProvider(IWindowsPowerApi powerApi)
    {
        _powerApi = powerApi;
    }

    public OperationResult<IReadOnlySet<NightAction>> ReadSupportedActions()
    {
        try
        {
            var capabilities = _powerApi.ReadCapabilities();
            var actions = new HashSet<NightAction>
            {
                NightAction.ShutDown,
                NightAction.Lock,
                NightAction.WarnOnly
            };

            if (capabilities.SleepSupported)
            {
                actions.Add(NightAction.Sleep);
            }

            if (capabilities.HibernateSupported)
            {
                actions.Add(NightAction.Hibernate);
            }

            return OperationResult<IReadOnlySet<NightAction>>.Success(actions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return OperationResult<IReadOnlySet<NightAction>>.Failure(
                "power.capabilities.unavailable",
                "Power.CapabilitiesUnavailable",
                ex.Message);
        }
    }
}
