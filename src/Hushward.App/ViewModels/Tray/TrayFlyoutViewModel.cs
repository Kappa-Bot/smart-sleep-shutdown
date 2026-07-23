using System.Windows.Input;
using Hushward.Application.Runtime;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.Core.Decisions;

namespace Hushward.App.ViewModels.Tray;

public sealed class TrayFlyoutViewModel : ObservableObject, IObserver<NightRuntimeSnapshot>, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly Action<Action> _marshalToUi;
    private NightRuntimeSnapshot _snapshot;

    public TrayFlyoutViewModel(
        RuntimeSnapshotPublisher snapshots,
        Action pauseToday,
        Action openMain,
        Action exit,
        Action<Action>? marshalToUi = null)
    {
        _snapshot = snapshots.Latest;
        _marshalToUi = marshalToUi ?? (action => action());
        PauseTodayCommand = new RelayCommand(pauseToday);
        OpenMainCommand = new RelayCommand(openMain);
        ExitCommand = new RelayCommand(exit);
        _subscription = snapshots.Subscribe(this);
    }

    public NightRuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public DecisionReasonCode? PrimaryReasonCode => Snapshot.PrimaryReason;
    public string PrimaryReason => ReasonTextPresenter.Present(PrimaryReasonCode);
    public string StateLabel => Snapshot.MonitoringState switch
    {
        RuntimeState.Disabled => UiText.TrayStateOff,
        RuntimeState.WaitingForWindow => UiText.TrayStateWaiting,
        RuntimeState.Protected => UiText.TrayStateProtected,
        RuntimeState.Warning => UiText.TrayStateWarning,
        RuntimeState.SafeMode => UiText.TrayStateDegraded,
        _ => UiText.TrayStateReady
    };

    public int ProtectionCount =>
        Snapshot.ProtectionSummary.Critical.Count +
        Snapshot.ProtectionSummary.Temporary.Count +
        Snapshot.ProtectionSummary.Contextual.Count;

    public ICommand PauseTodayCommand { get; }
    public ICommand OpenMainCommand { get; }
    public ICommand ExitCommand { get; }

    public void OnNext(NightRuntimeSnapshot value) =>
        _marshalToUi(() =>
        {
            Snapshot = value;
            OnPropertyChanged(nameof(PrimaryReasonCode));
            OnPropertyChanged(nameof(PrimaryReason));
            OnPropertyChanged(nameof(StateLabel));
            OnPropertyChanged(nameof(ProtectionCount));
        });

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose() => _subscription.Dispose();
}
