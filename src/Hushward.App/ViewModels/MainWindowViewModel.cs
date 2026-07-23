using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Hushward.App.Localization;
using Hushward.App.Runtime;
using Hushward.Application.Runtime;
using Hushward.App.Settings;
using Hushward.Core.Abstractions;
using Hushward.Core.Models;
using Hushward.Core.Services;

namespace Hushward.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IIdleDetector? _idleDetector;
    private readonly IContextDetector? _contextDetector;
    private readonly IShutdownExecutor? _shutdownExecutor;
    private readonly ISystemClock? _clock;
    private readonly Action<Action> _marshalToUi;
    private readonly IUserSettingsStore? _settingsStore;
    private readonly IWarningSessionController? _warningSession;
    private readonly RuntimeSnapshotPublisher? _runtimeSnapshots;
    private readonly DecisionEngine _decisionEngine = new();
    private readonly RelayCommand _cancelShutdownCommand;
    private readonly RelayCommand _disableUntilTomorrowCommand;

    private CancellationTokenSource? _monitoringCancellation;
    private CancellationTokenSource? _temporaryDisableCancellation;
    private bool _disposed;
    private bool _resumeAfterTemporaryDisable;
    private bool _isEnabled;
    private string _statusText = UiText.StatusDisabled;
    private string _trayStatusText = UiText.TrayStatusDisabled;
    private string _settingsWarningText = string.Empty;
    private string _startTimeText = "01:00";
    private int _idleThresholdMinutes = 15;
    private bool _contextChecksEnabled = true;
    private bool _isCountdownActive;
    private int _countdownSecondsRemaining;
    private DateTimeOffset? _temporarilyDisabledUntil;
    private bool _isLoadingSettings;

    public MainWindowViewModel(
        IIdleDetector? idleDetector = null,
        IContextDetector? contextDetector = null,
        IShutdownExecutor? shutdownExecutor = null,
        ISystemClock? clock = null,
        Action<Action>? marshalToUi = null,
        IUserSettingsStore? settingsStore = null,
        IWarningSessionController? warningSession = null,
        RuntimeSnapshotPublisher? runtimeSnapshots = null)
    {
        _idleDetector = idleDetector;
        _contextDetector = contextDetector;
        _shutdownExecutor = shutdownExecutor;
        _clock = clock;
        _marshalToUi = marshalToUi ?? (action => action());
        _settingsStore = settingsStore;
        _warningSession = warningSession;
        _runtimeSnapshots = runtimeSnapshots;
        _cancelShutdownCommand = new RelayCommand(CancelCountdownByUser, () => IsCountdownActive);
        _disableUntilTomorrowCommand = new RelayCommand(DisableUntilTomorrow, () => !IsTemporarilyDisabled);

        LoadSettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                OnPropertyChanged(nameof(HeaderStatusBrush));

                if (value && IsTemporarilyDisabled)
                {
                    ClearTemporaryDisable();
                }

                if (value)
                {
                    StartMonitoring();
                }
                else
                {
                    StopMonitoring();
                }

                UpdateTrayStatus();
                SaveSettings();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetField(ref _statusText, value))
            {
                UpdateTrayStatus();
                PublishRuntimeState();
            }
        }
    }

    public string TrayStatusText
    {
        get => _trayStatusText;
        private set => SetField(ref _trayStatusText, value);
    }

    public string HeaderStatusBrush => IsTemporarilyDisabled
        ? "#F59E0B"
        : IsEnabled
            ? "#16A34A"
            : "#64748B";

    public string SettingsWarningText
    {
        get => _settingsWarningText;
        private set
        {
            if (SetField(ref _settingsWarningText, value))
            {
                OnPropertyChanged(nameof(IsSettingsWarningVisible));
            }
        }
    }

    public bool IsSettingsWarningVisible => !string.IsNullOrWhiteSpace(SettingsWarningText);

    public string StartTimeText
    {
        get => _startTimeText;
        set
        {
            if (SetField(ref _startTimeText, value))
            {
                OnPropertyChanged(nameof(ScheduleSummaryText));
                ApplySettingsChange();
                SaveSettings();
            }
        }
    }

    public int IdleThresholdMinutes
    {
        get => _idleThresholdMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 240);
            if (SetField(ref _idleThresholdMinutes, clamped))
            {
                OnPropertyChanged(nameof(ScheduleSummaryText));
                ApplySettingsChange();
                SaveSettings();
            }
        }
    }

    public bool ContextChecksEnabled
    {
        get => _contextChecksEnabled;
        set
        {
            if (SetField(ref _contextChecksEnabled, value))
            {
                OnPropertyChanged(nameof(ScheduleSummaryText));
                ApplySettingsChange();
                SaveSettings();
            }
        }
    }

    public string ScheduleSummaryText
    {
        get
        {
            var start = TimeOnly.TryParse(StartTimeText, out var parsedStart)
                ? parsedStart.ToString("HH:mm")
                : "HH:mm";
            var context = ContextChecksEnabled ? UiText.ContextOn : UiText.ContextOff;
            return Format(UiText.ScheduleSummaryFormat, start, IdleThresholdMinutes, context);
        }
    }

    public bool IsCountdownActive
    {
        get => _isCountdownActive;
        private set
        {
            if (SetField(ref _isCountdownActive, value))
            {
                _cancelShutdownCommand.RaiseCanExecuteChanged();
                PublishRuntimeState();
            }
        }
    }

    public int CountdownSecondsRemaining
    {
        get => _countdownSecondsRemaining;
        private set => SetField(ref _countdownSecondsRemaining, value);
    }

    public bool IsTemporarilyDisabled => TemporarilyDisabledUntil is not null;

    public DateTimeOffset? TemporarilyDisabledUntil
    {
        get => _temporarilyDisabledUntil;
        private set
        {
            if (SetField(ref _temporarilyDisabledUntil, value))
            {
                OnPropertyChanged(nameof(IsTemporarilyDisabled));
                OnPropertyChanged(nameof(HeaderStatusBrush));
                _disableUntilTomorrowCommand.RaiseCanExecuteChanged();
                UpdateTrayStatus();
                SaveSettings();
            }
        }
    }

    public ICommand CancelShutdownCommand => _cancelShutdownCommand;

    public ICommand DisableUntilTomorrowCommand => _disableUntilTomorrowCommand;

    public SleepShutdownSettings CreateSettings()
    {
        RefreshTemporaryDisableStatus();

        var hasValidStartTime = TimeOnly.TryParse(StartTimeText, out var startTime);

        return SleepShutdownSettings.Default with
        {
            Enabled = IsEnabled && !IsTemporarilyDisabled && hasValidStartTime,
            StartTime = startTime,
            IdleThreshold = TimeSpan.FromMinutes(Math.Clamp(IdleThresholdMinutes, 1, 240)),
            ContextChecksEnabled = ContextChecksEnabled
        };
    }

    public void CancelCountdownFromInput()
    {
        if (!IsCountdownActive)
        {
            return;
        }

        _decisionEngine.CancelAndRequireRearm();
        _ = _warningSession?.InvalidateForInputAsync();
        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;
        StatusText = UiText.StatusCancelledActivity;
    }

    public void DisableUntilTomorrow()
    {
        RefreshTemporaryDisableStatus();

        var now = CurrentTime;
        _resumeAfterTemporaryDisable = IsEnabled;
        TemporarilyDisabledUntil = new DateTimeOffset(now.Date.AddDays(1), now.Offset);
        IsEnabled = false;
        StatusText = UiText.StatusPausedTomorrow;

        if (_idleDetector is not null || _contextDetector is not null || _shutdownExecutor is not null)
        {
            StartTemporaryDisableWatcher();
        }

        SaveSettings();
    }

    public void ReactivateToday()
    {
        ClearTemporaryDisable();
        IsEnabled = true;
        StatusText = UiText.StatusWatching;
    }

    public void RunScheduledCheck()
    {
        RefreshTemporaryDisableStatus();

        if (IsEnabled && !IsTemporarilyDisabled)
        {
            StartMonitoring();
        }
    }

    public void RefreshTemporaryDisableStatus()
    {
        if (TemporarilyDisabledUntil is null || CurrentTime < TemporarilyDisabledUntil.Value)
        {
            return;
        }

        var shouldResume = _resumeAfterTemporaryDisable;
        ClearTemporaryDisable();

        if (shouldResume)
        {
            IsEnabled = true;
        }
        else
        {
            StatusText = UiText.StatusDisabled;
            UpdateTrayStatus();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopMonitoring();
        StopTemporaryDisableWatcher();
    }

    private void StartMonitoring()
    {
        StatusText = UiText.StatusWatching;

        if (_idleDetector is null || _contextDetector is null || _shutdownExecutor is null || _clock is null)
        {
            return;
        }

        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = new CancellationTokenSource();
        _ = MonitorAsync(_monitoringCancellation.Token);
    }

    private void StopMonitoring()
    {
        _ = _warningSession?.InvalidateForProtectionAsync();
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _decisionEngine.Disable();
        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;
        StatusText = UiText.StatusDisabled;
    }

    private void StartTemporaryDisableWatcher()
    {
        StopTemporaryDisableWatcher();
        _temporaryDisableCancellation = new CancellationTokenSource();
        _ = WatchTemporaryDisableAsync(_temporaryDisableCancellation.Token);
    }

    private void StopTemporaryDisableWatcher()
    {
        _temporaryDisableCancellation?.Cancel();
        _temporaryDisableCancellation?.Dispose();
        _temporaryDisableCancellation = null;
    }

    private async Task WatchTemporaryDisableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var disabledUntil = TemporarilyDisabledUntil;
            if (disabledUntil is null)
            {
                return;
            }

            var delay = disabledUntil.Value - CurrentTime;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            ApplyUi(RefreshTemporaryDisableStatus);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!TimeOnly.TryParse(StartTimeText, out _))
                {
                    ApplyUi(() =>
                    {
                        IsCountdownActive = false;
                        CountdownSecondsRemaining = 0;
                        StatusText = UiText.StatusInvalidTime;
                    });
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var settings = CreateSettings();
                var now = CurrentTime;
                var delayBeforeEvaluation = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);
                if (delayBeforeEvaluation > TimeSpan.Zero)
                {
                    var wakeTime = now + delayBeforeEvaluation;
                    ApplyUi(() => StatusText = delayBeforeEvaluation > MonitoringSchedule.PrecheckLeadTime
                        ? Format(UiText.StatusSleepingUntilFormat, wakeTime)
                        : Format(UiText.StatusReadyForFormat, settings.StartTime));
                    await Task.Delay(delayBeforeEvaluation, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var nextDelay = await EvaluateOnceAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(nextDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (_warningSession is not null)
            {
                await _warningSession.InvalidateForProtectionAsync().ConfigureAwait(false);
            }

            ApplyUi(() =>
            {
                _decisionEngine.CancelAndRequireRearm();
                IsCountdownActive = false;
                CountdownSecondsRemaining = 0;
                StatusText = UiText.StatusMonitoringPaused;
            });
        }
    }

    private async Task<TimeSpan> EvaluateOnceAsync(CancellationToken cancellationToken)
    {
        var settings = CreateSettings();
        if (!settings.Enabled || _idleDetector is null || _contextDetector is null || _shutdownExecutor is null || _clock is null)
        {
            return TimeSpan.FromMinutes(5);
        }

        IdleSnapshot idle;
        try
        {
            idle = await _idleDetector.GetIdleSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _decisionEngine.CancelAndRequireRearm();
            ApplyUi(() =>
            {
                IsCountdownActive = false;
                CountdownSecondsRemaining = 0;
                StatusText = UiText.StatusDetectorBlocked;
            });
            return TimeSpan.FromMinutes(1);
        }

        var context = settings.ContextChecksEnabled
            ? await _contextDetector.GetCurrentContextAsync(cancellationToken).ConfigureAwait(false)
            : ContextSnapshot.Clear;

        var now = _clock.Now;
        var result = _decisionEngine.Evaluate(settings, idle, context, now);

        switch (result.Action)
        {
            case ShutdownDecisionAction.StartWarning:
                if (_warningSession is not null)
                {
                    await _warningSession
                        .StartAsync(settings.WarningDuration, cancellationToken)
                        .ConfigureAwait(false);
                }

                ApplyUi(() =>
                {
                    IsCountdownActive = true;
                    CountdownSecondsRemaining = (int)settings.WarningDuration.TotalSeconds;
                    StatusText = UiText.StatusShutdownCountdown;
                });
                break;

            case ShutdownDecisionAction.CancelWarning:
                if (_warningSession is not null)
                {
                    await _warningSession.InvalidateForProtectionAsync().ConfigureAwait(false);
                }

                ApplyUi(() =>
                {
                    IsCountdownActive = false;
                    CountdownSecondsRemaining = 0;
                    StatusText = UiText.StatusWatching;
                });
                break;

            case ShutdownDecisionAction.ShutdownNow:
                ApplyUi(() =>
                {
                    IsCountdownActive = false;
                    CountdownSecondsRemaining = 0;
                    StatusText = UiText.StatusShuttingDown;
                });
                await _shutdownExecutor.ShutdownNowAsync(cancellationToken).ConfigureAwait(false);
                break;

            case ShutdownDecisionAction.None:
                ApplyUi(() => UpdatePassiveStatus(settings, idle, context, now, result));
                break;
        }

        return MonitoringSchedule.GetDelayAfterEvaluation(settings, idle, result.State);
    }

    private void UpdatePassiveStatus(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now,
        DecisionResult result)
    {
        if (result.State == DecisionState.Warning && result.WarningStartedAt is not null)
        {
            var elapsed = now - result.WarningStartedAt.Value;
            var remaining = settings.WarningDuration - elapsed;
            IsCountdownActive = true;
            CountdownSecondsRemaining = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
            StatusText = UiText.StatusShutdownCountdown;
            return;
        }

        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;

        if (!MonitoringSchedule.IsInsideEvaluationWindow(settings, now))
        {
            StatusText = Format(UiText.StatusWaitingUntilFormat, settings.StartTime);
        }
        else if (ContextBlockingPolicy.BlocksShutdown(settings, idle, context))
        {
            var reason = context.Blockers.FirstOrDefault()?.Description;
            StatusText = string.IsNullOrWhiteSpace(reason)
                ? UiText.StatusBlockedActivity
                : Format(UiText.StatusBlockedFormat, reason);
        }
        else
        {
            StatusText = idle.IdleDuration > settings.IdleThreshold
                ? UiText.StatusReadyToWarn
                : Format(
                    UiText.StatusIdleProgressFormat,
                    (int)idle.IdleDuration.TotalMinutes,
                    (int)settings.IdleThreshold.TotalMinutes);
        }
    }

    private void CancelCountdownByUser()
    {
        CancelCountdownFromInput();
    }

    private void ApplySettingsChange()
    {
        if (IsCountdownActive)
        {
            _decisionEngine.CancelAndRequireRearm();
            _ = _warningSession?.InvalidateForProtectionAsync();
            IsCountdownActive = false;
            CountdownSecondsRemaining = 0;
            StatusText = UiText.StatusWatching;
        }

        if (IsEnabled && !IsTemporarilyDisabled)
        {
            StartMonitoring();
        }
    }

    private void ApplyUi(Action action)
    {
        _marshalToUi(action);
    }

    private void LoadSettings()
    {
        var snapshot = _settingsStore?.Load();
        if (snapshot is null)
        {
            return;
        }

        _isLoadingSettings = true;
        _isEnabled = snapshot.IsEnabled;
        _startTimeText = string.IsNullOrWhiteSpace(snapshot.StartTimeText)
            ? SleepShutdownSettings.Default.StartTime.ToString("HH:mm")
            : snapshot.StartTimeText;
        _idleThresholdMinutes = Math.Clamp(snapshot.IdleThresholdMinutes, 1, 240);
        _contextChecksEnabled = snapshot.ContextChecksEnabled;
        _temporarilyDisabledUntil = snapshot.TemporarilyDisabledUntil;
        _resumeAfterTemporaryDisable = snapshot.ResumeAfterTemporaryDisable;
        _isLoadingSettings = false;

        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(StartTimeText));
        OnPropertyChanged(nameof(IdleThresholdMinutes));
        OnPropertyChanged(nameof(ContextChecksEnabled));
        OnPropertyChanged(nameof(ScheduleSummaryText));
        OnPropertyChanged(nameof(TemporarilyDisabledUntil));
        OnPropertyChanged(nameof(IsTemporarilyDisabled));
        OnPropertyChanged(nameof(HeaderStatusBrush));

        RefreshTemporaryDisableStatus();
        UpdateTrayStatus();

        if (IsEnabled && !IsTemporarilyDisabled)
        {
            StartMonitoring();
        }
        else if (IsTemporarilyDisabled)
        {
            StatusText = UiText.StatusPausedTomorrow;
            StartTemporaryDisableWatcher();
        }
    }

    private void SaveSettings()
    {
        if (_isLoadingSettings || _settingsStore is null)
        {
            return;
        }

        try
        {
            _settingsStore.Save(new UserSettingsSnapshot(
                IsEnabled,
                StartTimeText,
                IdleThresholdMinutes,
                ContextChecksEnabled,
                TemporarilyDisabledUntil,
                _resumeAfterTemporaryDisable));
            SettingsWarningText = string.Empty;
        }
        catch (IOException)
        {
            SettingsWarningText = UiText.SettingsSaveFailed;
        }
        catch (UnauthorizedAccessException)
        {
            SettingsWarningText = UiText.SettingsSaveFailed;
        }
    }

    private void ClearTemporaryDisable()
    {
        StopTemporaryDisableWatcher();
        _resumeAfterTemporaryDisable = false;
        TemporarilyDisabledUntil = null;
        UpdateTrayStatus();
        SaveSettings();
    }

    private DateTimeOffset CurrentTime => _clock?.Now ?? DateTimeOffset.Now;

    private void UpdateTrayStatus()
    {
        if (IsTemporarilyDisabled)
        {
            TrayStatusText = UiText.TrayStatusPausedTomorrow;
        }
        else if (IsEnabled)
        {
            TrayStatusText = Format(UiText.TrayStatusActiveFormat, StatusText);
        }
        else
        {
            TrayStatusText = UiText.TrayStatusDisabled;
        }
    }

    private void PublishRuntimeState()
    {
        if (_runtimeSnapshots is null)
        {
            return;
        }

        var latest = _runtimeSnapshots.Latest;
        var state = ResolveRuntimeState();
        var code = IsTemporarilyDisabled
            ? "status.paused-today"
            : state switch
            {
                RuntimeState.Disabled => "status.disabled",
                RuntimeState.WaitingForWindow => "status.waiting-window",
                RuntimeState.Protected => "status.protected",
                RuntimeState.Warning => "status.warning",
                RuntimeState.Executing => "status.executing",
                RuntimeState.SafeMode => "status.safe-mode",
                _ => "status.monitoring"
            };
        var now = CurrentTime;
        _runtimeSnapshots.Publish(latest with
        {
            Sequence = latest.Sequence + 1,
            CapturedAt = now,
            MonitoringState = state,
            LastMeaningfulEvent = new RuntimeEvent(code, now, latest.PrimaryReason)
        });
    }

    private RuntimeState ResolveRuntimeState()
    {
        if (!IsEnabled || IsTemporarilyDisabled)
        {
            return RuntimeState.Disabled;
        }

        if (IsCountdownActive)
        {
            return RuntimeState.Warning;
        }

        if (StatusText.StartsWith(UiText.StatusBlockedFormat.Split('{')[0], StringComparison.CurrentCulture) ||
            StatusText == UiText.StatusBlockedActivity ||
            StatusText == UiText.StatusDetectorBlocked)
        {
            return RuntimeState.Protected;
        }

        if (StatusText.StartsWith(UiText.StatusSleepingUntilFormat.Split('{')[0], StringComparison.CurrentCulture) ||
            StatusText.StartsWith(UiText.StatusReadyForFormat.Split('{')[0], StringComparison.CurrentCulture) ||
            StatusText.StartsWith(UiText.StatusWaitingUntilFormat.Split('{')[0], StringComparison.CurrentCulture))
        {
            return RuntimeState.WaitingForWindow;
        }

        return StatusText == UiText.StatusShuttingDown
            ? RuntimeState.Executing
            : RuntimeState.Monitoring;
    }

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
