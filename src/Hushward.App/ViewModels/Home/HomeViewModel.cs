using Hushward.Application.Runtime;
using Hushward.App.Localization;
using Hushward.App.Presentation;

namespace Hushward.App.ViewModels.Home;

public sealed class HomeViewModel : ObservableObject
{
    private NightRuntimeSnapshot _snapshot;

    public HomeViewModel(NightRuntimeSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public string StateLabel => Snapshot.MonitoringState switch
    {
        RuntimeState.Disabled => UiText.HomeStateOff,
        RuntimeState.WaitingForWindow => UiText.HomeStateWaiting,
        RuntimeState.Protected => UiText.HomeStateProtected,
        RuntimeState.Warning => UiText.HomeStateWarning,
        RuntimeState.SafeMode => UiText.HomeStateDegraded,
        _ => UiText.HomeStateReady
    };

    public string PrimaryReason => ReasonTextPresenter.Present(Snapshot.PrimaryReason);
    public int ActiveProtectionCount =>
        Snapshot.ProtectionSummary.Critical.Count +
        Snapshot.ProtectionSummary.Temporary.Count +
        Snapshot.ProtectionSummary.Contextual.Count;

    public NightRuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public void Update(NightRuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(PrimaryReason));
        OnPropertyChanged(nameof(ActiveProtectionCount));
    }
}
