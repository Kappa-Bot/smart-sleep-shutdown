using System.ComponentModel;
using System.Windows.Input;
using Hushward.Application.Runtime;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.App.ViewModels.Home;
using Hushward.App.ViewModels.Protections;
using Hushward.App.ViewModels.Routines;
using Hushward.App.ViewModels.Tonight;
using Hushward.Core.Actions;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;
using Hushward.App.Runtime;

namespace Hushward.App.ViewModels;

public sealed class ShellViewModel : ObservableObject, IObserver<NightRuntimeSnapshot>, IDisposable
{
    private readonly NightMonitorController _monitor;
    private readonly Action<Action> _marshalToUi;
    private readonly Func<NightRoutine, Task>? _synchronizeSchedule;
    private readonly IDisposable _snapshotSubscription;
    private NightRuntimeSnapshot _snapshot;

    public ShellViewModel(
        NightMonitorController monitor,
        RuntimeSnapshotPublisher snapshots,
        Action<Action> marshalToUi,
        Func<NightRoutine, Task>? synchronizeSchedule = null)
    {
        _monitor = monitor;
        _marshalToUi = marshalToUi;
        _synchronizeSchedule = synchronizeSchedule;
        _snapshot = snapshots.Latest;
        var routine = CreateRoutineFromMonitor();
        Home = new HomeViewModel(_snapshot);
        Tonight = new TonightViewModel(routine, DateTimeOffset.Now, ApplyTonightOverride);
        Routines = new RoutinesViewModel([routine], TimeZoneInfo.Local);
        Protections = new ProtectionsViewModel(_snapshot);
        _monitor.PropertyChanged += OnMonitorPropertyChanged;
        _snapshotSubscription = snapshots.Subscribe(this);
        RequestScheduleSync();
    }

    public NightRuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public HomeViewModel Home { get; }
    public TonightViewModel Tonight { get; }
    public RoutinesViewModel Routines { get; }
    public ProtectionsViewModel Protections { get; }

    public string StatusText => Snapshot.MonitoringState switch
    {
        RuntimeState.Disabled when Snapshot.LastMeaningfulEvent?.Code == "status.paused-until" =>
            string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                UiText.StatusPausedUntilFormat,
                Snapshot.NextEvaluationAt),
        RuntimeState.Disabled when IsTemporarilyDisabled => UiText.StatusPausedTomorrow,
        RuntimeState.Disabled => UiText.StatusDisabled,
        RuntimeState.WaitingForWindow => UiText.StatusWaitingForWindow,
        RuntimeState.Protected => UiText.StatusBlockedActivity,
        RuntimeState.Warning => UiText.StatusShutdownCountdown,
        RuntimeState.Executing => UiText.StatusShuttingDown,
        RuntimeState.SafeMode => UiText.StatusMonitoringPaused,
        _ => UiText.StatusWatching
    };

    public string TrayStatusText => IsTemporarilyDisabled
        ? Snapshot.LastMeaningfulEvent?.Code == "status.paused-until"
            ? string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                UiText.TrayStatusPausedUntilFormat,
                Snapshot.NextEvaluationAt)
            : UiText.TrayStatusPausedTomorrow
        : IsEnabled
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, UiText.TrayStatusActiveFormat, StatusText)
            : UiText.TrayStatusDisabled;

    public string HeaderStatusBrush => IsTemporarilyDisabled
        ? "#F2B84B"
        : IsEnabled
            ? "#66C7A5"
            : "#98A3B8";
    public bool IsSettingsWarningVisible => _monitor.IsSettingsWarningVisible;
    public string SettingsWarningText => _monitor.SettingsWarningText;
    public bool IsEnabled
    {
        get => Snapshot.MonitoringState is not RuntimeState.Disabled and not RuntimeState.SafeMode;
        set
        {
            _monitor.IsEnabled = value;
            RequestScheduleSync();
        }
    }

    public string ScheduleSummaryText => _monitor.ScheduleSummaryText;
    public string StartTimeText
    {
        get => _monitor.StartTimeText;
        set
        {
            _monitor.StartTimeText = value;
            RequestScheduleSync();
        }
    }

    public int IdleThresholdMinutes
    {
        get => _monitor.IdleThresholdMinutes;
        set
        {
            _monitor.IdleThresholdMinutes = value;
            RequestScheduleSync();
        }
    }

    public bool ContextChecksEnabled
    {
        get => _monitor.ContextChecksEnabled;
        set => _monitor.ContextChecksEnabled = value;
    }

    public bool WakeEnabled
    {
        get => _monitor.WakeEnabled;
        set
        {
            _monitor.WakeEnabled = value;
            RequestScheduleSync();
        }
    }

    public bool IsCountdownActive => Snapshot.WarningState.Kind == WarningStateKind.Active;
    public int CountdownSecondsRemaining
    {
        get
        {
            if (!IsCountdownActive ||
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

    public bool IsTemporarilyDisabled =>
        Snapshot.LastMeaningfulEvent?.Code is "status.paused-today" or "status.paused-until";
    public ICommand CancelShutdownCommand => _monitor.CancelShutdownCommand;
    public ICommand DisableUntilTomorrowCommand => _monitor.DisableUntilTomorrowCommand;

    public void CancelCountdownFromInput() => _monitor.CancelCountdownFromInput();

    public void DisableUntilTomorrow()
    {
        _monitor.DisableUntilTomorrow();
        RequestScheduleSync();
    }

    public void Postpone(int minutes)
    {
        _monitor.DisableFor(TimeSpan.FromMinutes(minutes));
        RequestScheduleSync();
    }

    public void ReactivateToday()
    {
        _monitor.ReactivateToday();
        RequestScheduleSync();
    }

    public void RefreshTemporaryDisableStatus() => _monitor.RefreshTemporaryDisableStatus();

    public void RunScheduledCheck() => _monitor.RunScheduledCheck();

    public void OnNext(NightRuntimeSnapshot value) =>
        _marshalToUi(() =>
        {
            Snapshot = value;
            Home.Update(value);
            Protections.Update(value);
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(TrayStatusText));
            OnPropertyChanged(nameof(HeaderStatusBrush));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(IsCountdownActive));
            OnPropertyChanged(nameof(CountdownSecondsRemaining));
            OnPropertyChanged(nameof(IsTemporarilyDisabled));
        });

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose()
    {
        _snapshotSubscription.Dispose();
        _monitor.PropertyChanged -= OnMonitorPropertyChanged;
        _monitor.Dispose();
    }

    private void OnMonitorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NightMonitorController.StatusText)
            or nameof(NightMonitorController.TrayStatusText)
            or nameof(NightMonitorController.HeaderStatusBrush)
            or nameof(NightMonitorController.IsEnabled)
            or nameof(NightMonitorController.IsCountdownActive)
            or nameof(NightMonitorController.IsTemporarilyDisabled))
        {
            return;
        }

        _marshalToUi(() => OnPropertyChanged(e.PropertyName));
    }

    public async Task ApplyRoutineAsync(NightRoutine routine)
    {
        _monitor.IsEnabled = false;
        _monitor.StartTimeText = routine.Window.Earliest.ToString("HH:mm");
        _monitor.IdleThresholdMinutes = (int)routine.MinimumIdle.TotalMinutes;
        _monitor.ContextChecksEnabled = true;
        _monitor.WakeEnabled = routine.WakePolicy != WakePolicy.NeverWake;
        _monitor.SelectedAction = routine.PrimaryAction;
        _monitor.IsEnabled = routine.Enabled;
        if (_synchronizeSchedule is not null)
        {
            await _synchronizeSchedule(CreateRoutineFromMonitor()).ConfigureAwait(false);
        }

    }

    private NightRoutine CreateRoutineFromMonitor() => new(
        Guid.NewGuid(),
        UiText.DefaultRoutineName,
        _monitor.IsEnabled,
        Enum.GetValues<DayOfWeek>(),
        new NightWindow(
            TimeOnly.TryParse(_monitor.StartTimeText, out var earliest) ? earliest : new TimeOnly(1, 0),
            new TimeOnly(6, 0)),
        TimeSpan.FromMinutes(_monitor.IdleThresholdMinutes),
        _monitor.SelectedAction,
        TimeSpan.FromSeconds(60),
        _monitor.WakeEnabled ? WakePolicy.WakeToEvaluate : WakePolicy.NeverWake,
        LatestDecisionPolicy.KeepWaitingForProtections,
        []);

    private void ApplyTonightOverride(TonightOverride tonightOverride)
    {
        if (tonightOverride.PauseUntilTomorrow ||
            tonightOverride.RequireManualConfirmation)
        {
            DisableUntilTomorrow();
        }
        else if (tonightOverride.PostponedUntil is { } postponedUntil)
        {
            _monitor.DisableFor(postponedUntil - DateTimeOffset.Now);
            RequestScheduleSync();
        }
    }

    private void RequestScheduleSync()
    {
        if (_synchronizeSchedule is not null)
        {
            _ = _synchronizeSchedule(CreateRoutineFromMonitor());
        }
    }
}
