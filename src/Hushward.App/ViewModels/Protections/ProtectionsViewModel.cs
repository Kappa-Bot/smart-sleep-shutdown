using Hushward.Application.Runtime;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.Core.Protections;

namespace Hushward.App.ViewModels.Protections;

public sealed class ProtectionsViewModel : ObservableObject
{
    private NightRuntimeSnapshot _snapshot;

    public ProtectionsViewModel(NightRuntimeSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public bool HasRequiredUnknownEvidence =>
        Snapshot.ProtectionSummary.Critical.Any(signal => signal.State == ObservationState.Unknown);

    public bool CanDisableRequiredProtection => false;
    public string Summary => HasRequiredUnknownEvidence
        ? UiText.ProtectionUnknownSummary
        : string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            UiText.ProtectionCountFormat,
            Snapshot.ProtectionSummary.Critical.Count,
            Snapshot.ProtectionSummary.Temporary.Count,
            Snapshot.ProtectionSummary.Contextual.Count);

    public NightRuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public void Update(NightRuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
        OnPropertyChanged(nameof(HasRequiredUnknownEvidence));
        OnPropertyChanged(nameof(Summary));
    }
}
