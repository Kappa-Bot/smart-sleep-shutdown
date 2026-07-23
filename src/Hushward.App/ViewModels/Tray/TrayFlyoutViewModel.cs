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
        Action toggleEnabled,
        Action openMain,
        Action exit,
        Action<Action>? marshalToUi = null)
    {
        _snapshot = snapshots.Latest;
        _marshalToUi = marshalToUi ?? (action => action());
        PauseTodayCommand = new RelayCommand(pauseToday);
        ToggleEnabledCommand = new RelayCommand(toggleEnabled);
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
        RuntimeState.Disabled when IsPaused => UiText.TrayStatusPausedTomorrow,
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

    public string IdleText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        UiText.TrayIdleFormat,
        Math.Max(0, (int)Snapshot.IdleState.IdleDuration.TotalMinutes));
    public string ToggleEnabledText =>
        IsPaused
            ? UiText.TrayEnableNow
            : Snapshot.MonitoringState is RuntimeState.Disabled or RuntimeState.SafeMode
            ? UiText.TrayEnable
            : UiText.TrayDisable;

    public ICommand PauseTodayCommand { get; }
    public ICommand ToggleEnabledCommand { get; }
    public ICommand OpenMainCommand { get; }
    public ICommand ExitCommand { get; }

    private bool IsPaused =>
        Snapshot.LastMeaningfulEvent?.Code is "status.paused-today" or "status.paused-until";

    public void OnNext(NightRuntimeSnapshot value) =>
        _marshalToUi(() =>
        {
            Snapshot = value;
            OnPropertyChanged(nameof(PrimaryReasonCode));
            OnPropertyChanged(nameof(PrimaryReason));
            OnPropertyChanged(nameof(StateLabel));
            OnPropertyChanged(nameof(ProtectionCount));
            OnPropertyChanged(nameof(IdleText));
            OnPropertyChanged(nameof(ToggleEnabledText));
        });

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose() => _subscription.Dispose();
}
