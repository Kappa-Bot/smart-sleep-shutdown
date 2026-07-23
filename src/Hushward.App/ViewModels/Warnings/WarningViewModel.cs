using System.Windows.Input;
using Hushward.Application.Runtime;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.App.Runtime;
using Hushward.Core.Actions;
using Hushward.Core.Warnings;

namespace Hushward.App.ViewModels.Warnings;

public sealed class WarningViewModel : ObservableObject, IObserver<NightRuntimeSnapshot>, IDisposable
{
    private readonly IWarningSessionController _controller;
    private readonly IDisposable _subscription;
    private readonly Action<Action> _marshalToUi;
    private NightRuntimeSnapshot _snapshot;

    public WarningViewModel(
        RuntimeSnapshotPublisher snapshots,
        IWarningSessionController controller,
        Action<int> postpone,
        Action keepActiveUntilTomorrow,
        Action<Action>? marshalToUi = null)
    {
        _snapshot = snapshots.Latest;
        _controller = controller;
        _marshalToUi = marshalToUi ?? (action => action());
        CancelCommand = new AsyncCommand(_ => HandleUserInputAsync());
        Postpone15Command = new AsyncCommand(_ => PostponeAsync(15, postpone));
        Postpone30Command = new AsyncCommand(_ => PostponeAsync(30, postpone));
        Postpone60Command = new AsyncCommand(_ => PostponeAsync(60, postpone));
        KeepActiveUntilTomorrowCommand = new AsyncCommand(_ => KeepActiveAsync(keepActiveUntilTomorrow));
        _subscription = snapshots.Subscribe(this);
    }

    public NightRuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public bool IsActive => Snapshot.WarningState.Kind == WarningStateKind.Active;
    public int RemainingSeconds
    {
        get
        {
            if (!IsActive ||
                Snapshot.WarningState.StartedAt is null ||
                Snapshot.Decision?.WarningDuration is null)
            {
                return 0;
            }

            var remaining = Snapshot.Decision.WarningDuration.Value -
                (DateTimeOffset.Now - Snapshot.WarningState.StartedAt.Value);
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }
    }

    public string RemainingText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        UiText.WarningSecondsFormat,
        RemainingSeconds);

    public string ActionText => Snapshot.Decision?.AuthorizedAction switch
    {
        NightAction.Hibernate => UiText.ActionHibernate,
        NightAction.Sleep => UiText.ActionSleep,
        NightAction.Lock => UiText.ActionLock,
        NightAction.WarnOnly => UiText.ActionWarnOnly,
        _ => UiText.ActionShutdown
    };

    public string ReasonText => ReasonTextPresenter.Present(Snapshot.PrimaryReason);
    public ICommand CancelCommand { get; }
    public ICommand Postpone15Command { get; }
    public ICommand Postpone30Command { get; }
    public ICommand Postpone60Command { get; }
    public ICommand KeepActiveUntilTomorrowCommand { get; }

    public Task HandleUserInputAsync() => _controller.InvalidateForInputAsync();

    public Task StartPreviewAsync() =>
        _controller.StartAsync(TimeSpan.FromSeconds(60), CancellationToken.None);

    private async Task PostponeAsync(int minutes, Action<int> postpone)
    {
        await _controller.InvalidateForInputAsync().ConfigureAwait(true);
        postpone(minutes);
    }

    private async Task KeepActiveAsync(Action keepActiveUntilTomorrow)
    {
        await _controller.InvalidateForInputAsync().ConfigureAwait(true);
        keepActiveUntilTomorrow();
    }

    public void Tick()
    {
        OnPropertyChanged(nameof(RemainingSeconds));
        OnPropertyChanged(nameof(RemainingText));
    }

    public void OnNext(NightRuntimeSnapshot value) =>
        _marshalToUi(() =>
        {
            Snapshot = value;
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(RemainingSeconds));
            OnPropertyChanged(nameof(RemainingText));
            OnPropertyChanged(nameof(ActionText));
            OnPropertyChanged(nameof(ReasonText));
        });

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose() => _subscription.Dispose();
}
