using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartSleepShutdown.App.Settings;
using SmartSleepShutdown.Core.Abstractions;
using SmartSleepShutdown.Core.Models;
using SmartSleepShutdown.Core.Services;

namespace SmartSleepShutdown.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IIdleDetector? _idleDetector;
    private readonly IContextDetector? _contextDetector;
    private readonly IShutdownExecutor? _shutdownExecutor;
    private readonly ISystemClock? _clock;
    private readonly Action<Action> _marshalToUi;
    private readonly IUserSettingsStore? _settingsStore;
    private readonly DecisionEngine _decisionEngine = new();
    private readonly RelayCommand _cancelShutdownCommand;
    private readonly RelayCommand _disableUntilTomorrowCommand;

    private CancellationTokenSource? _monitoringCancellation;
    private CancellationTokenSource? _temporaryDisableCancellation;
    private bool _disposed;
    private bool _resumeAfterTemporaryDisable;
    private bool _isEnabled;
    private string _statusText = "Desactivado";
    private string _trayStatusText = "Smart Sleep Shutdown - DESACTIVADO";
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
        IUserSettingsStore? settingsStore = null)
    {
        _idleDetector = idleDetector;
        _contextDetector = contextDetector;
        _shutdownExecutor = shutdownExecutor;
        _clock = clock;
        _marshalToUi = marshalToUi ?? (action => action());
        _settingsStore = settingsStore;
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
            }
        }
    }

    public string TrayStatusText
    {
        get => _trayStatusText;
        private set => SetField(ref _trayStatusText, value);
    }

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
                ApplySettingsChange();
                SaveSettings();
            }
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
        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;
        StatusText = "Cancelado por actividad";
    }

    public void DisableUntilTomorrow()
    {
        RefreshTemporaryDisableStatus();

        var now = CurrentTime;
        _resumeAfterTemporaryDisable = IsEnabled;
        TemporarilyDisabledUntil = new DateTimeOffset(now.Date.AddDays(1), now.Offset);
        IsEnabled = false;
        StatusText = "Pausado hasta manana";

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
        StatusText = "Vigilando";
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
            StatusText = "Desactivado";
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
        StatusText = "Vigilando";

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
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _decisionEngine.Disable();
        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;
        StatusText = "Desactivado";
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
                        StatusText = "Usa hora HH:mm";
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
                        ? $"Durmiendo hasta {wakeTime:HH:mm}"
                        : $"Listo para {settings.StartTime:HH:mm}");
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
            ApplyUi(() =>
            {
                _decisionEngine.CancelAndRequireRearm();
                IsCountdownActive = false;
                CountdownSecondsRemaining = 0;
                StatusText = "Vigilancia pausada";
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
                StatusText = "Bloqueado: detector";
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
                ApplyUi(() =>
                {
                    IsCountdownActive = true;
                    CountdownSecondsRemaining = (int)settings.WarningDuration.TotalSeconds;
                    StatusText = "Apagado en 60 segundos";
                });
                break;

            case ShutdownDecisionAction.CancelWarning:
                ApplyUi(() =>
                {
                    IsCountdownActive = false;
                    CountdownSecondsRemaining = 0;
                    StatusText = "Vigilando";
                });
                break;

            case ShutdownDecisionAction.ShutdownNow:
                ApplyUi(() =>
                {
                    IsCountdownActive = false;
                    CountdownSecondsRemaining = 0;
                    StatusText = "Apagando";
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
            StatusText = "Apagado en 60 segundos";
            return;
        }

        IsCountdownActive = false;
        CountdownSecondsRemaining = 0;

        if (!MonitoringSchedule.IsInsideEvaluationWindow(settings, now))
        {
            StatusText = $"Esperando hasta {settings.StartTime:HH:mm}";
        }
        else if (settings.ContextChecksEnabled && context.HasBlockingContext)
        {
            var reason = context.Blockers.FirstOrDefault()?.Description;
            StatusText = string.IsNullOrWhiteSpace(reason)
                ? "Bloqueado por actividad"
                : $"Bloqueado: {reason}";
        }
        else
        {
            StatusText = idle.IdleDuration > settings.IdleThreshold
                ? "Listo para avisar"
                : $"Inactivo {(int)idle.IdleDuration.TotalMinutes}/{(int)settings.IdleThreshold.TotalMinutes} min";
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
            IsCountdownActive = false;
            CountdownSecondsRemaining = 0;
            StatusText = "Vigilando";
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
        OnPropertyChanged(nameof(TemporarilyDisabledUntil));
        OnPropertyChanged(nameof(IsTemporarilyDisabled));

        RefreshTemporaryDisableStatus();
        UpdateTrayStatus();

        if (IsEnabled && !IsTemporarilyDisabled)
        {
            StartMonitoring();
        }
        else if (IsTemporarilyDisabled)
        {
            StatusText = "Pausado hasta manana";
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
            SettingsWarningText = "No se pudo guardar la configuracion";
        }
        catch (UnauthorizedAccessException)
        {
            SettingsWarningText = "No se pudo guardar la configuracion";
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
            TrayStatusText = "Smart Sleep Shutdown - PAUSADO hasta manana";
        }
        else if (IsEnabled)
        {
            TrayStatusText = "Smart Sleep Shutdown - ACTIVO";
        }
        else
        {
            TrayStatusText = "Smart Sleep Shutdown - DESACTIVADO";
        }
    }

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
