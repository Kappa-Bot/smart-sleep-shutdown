using SmartSleepShutdown.App.ViewModels;
using SmartSleepShutdown.App;
using SmartSleepShutdown.App.Settings;
using SmartSleepShutdown.Core.Abstractions;
using SmartSleepShutdown.Core.Models;
using System.Globalization;

namespace SmartSleepShutdown.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void DefaultsMatchProductPlan()
    {
        var viewModel = new MainWindowViewModel();

        Assert.False(viewModel.IsEnabled);
        Assert.Equal("01:00", viewModel.StartTimeText);
        Assert.Equal(15, viewModel.IdleThresholdMinutes);
        Assert.True(viewModel.ContextChecksEnabled);
        Assert.Equal("Desactivado", viewModel.StatusText);
    }

    [Fact]
    public void SettingsReflectViewModelValues()
    {
        var viewModel = new MainWindowViewModel
        {
            IsEnabled = true,
            StartTimeText = "02:30",
            IdleThresholdMinutes = 20,
            ContextChecksEnabled = false
        };

        SleepShutdownSettings settings = viewModel.CreateSettings();

        Assert.True(settings.Enabled);
        Assert.Equal(new TimeOnly(2, 30), settings.StartTime);
        Assert.Equal(TimeSpan.FromMinutes(20), settings.IdleThreshold);
        Assert.False(settings.ContextChecksEnabled);
        Assert.Equal("Activo desde 02:30 hasta 06:00 | Inactividad 20 min | Contexto off", viewModel.ScheduleSummaryText);
    }

    [Fact]
    public void ToggleTextIsClearSpanish()
    {
        Assert.Equal("Activado", BooleanBoxes.OnOffConverter.Convert(true, typeof(string), null!, CultureInfo.InvariantCulture));
        Assert.Equal("Desactivado", BooleanBoxes.OnOffConverter.Convert(false, typeof(string), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DisableDuringCountdownResetsWarningState()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero));
        var idleDetector = new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromHours(2), false));
        var shutdownExecutor = new FakeShutdownExecutor();
        var viewModel = new MainWindowViewModel(
            idleDetector,
            new ClearContextDetector(),
            shutdownExecutor,
            clock,
            action => action());

        viewModel.IsEnabled = true;
        await WaitUntilAsync(() => viewModel.IsCountdownActive);

        viewModel.IsEnabled = false;
        clock.Now = clock.Now.AddSeconds(61);
        viewModel.IsEnabled = true;
        await WaitUntilAsync(() => viewModel.IsCountdownActive);

        Assert.Equal(0, shutdownExecutor.CallCount);
        Assert.Equal(60, viewModel.CountdownSecondsRemaining);
    }

    [Fact]
    public void TemporarilyDisableUntilTomorrowTurnsOffAndShowsPausedTrayStatus()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 22, 30, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(clock: clock);
        viewModel.IsEnabled = true;

        viewModel.DisableUntilTomorrow();

        Assert.False(viewModel.IsEnabled);
        Assert.True(viewModel.IsTemporarilyDisabled);
        Assert.Equal(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero), viewModel.TemporarilyDisabledUntil);
        Assert.Equal("Pausado hasta manana", viewModel.StatusText);
        Assert.Equal("Smart Sleep Shutdown - PAUSADO hasta manana", viewModel.TrayStatusText);
        Assert.False(viewModel.CreateSettings().Enabled);
    }

    [Fact]
    public void TemporaryDisableRestoresActiveStateOnNextDay()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 22, 30, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(clock: clock);
        viewModel.IsEnabled = true;
        viewModel.DisableUntilTomorrow();

        clock.Now = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero);
        viewModel.RefreshTemporaryDisableStatus();

        Assert.True(viewModel.IsEnabled);
        Assert.False(viewModel.IsTemporarilyDisabled);
        Assert.Null(viewModel.TemporarilyDisabledUntil);
        Assert.Equal("Smart Sleep Shutdown - ACTIVO - Vigilando", viewModel.TrayStatusText);
    }

    [Fact]
    public void TemporaryDisableKeepsOffStateOnNextDayWhenItWasAlreadyOff()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 22, 30, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(clock: clock);

        viewModel.DisableUntilTomorrow();

        clock.Now = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero);
        viewModel.RefreshTemporaryDisableStatus();

        Assert.False(viewModel.IsEnabled);
        Assert.False(viewModel.IsTemporarilyDisabled);
        Assert.Equal("Smart Sleep Shutdown - DESACTIVADO", viewModel.TrayStatusText);
    }

    [Fact]
    public void TrayVisualStateShowsActiveSuspendedAndOff()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 22, 30, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(clock: clock);

        Assert.Equal(TrayVisualState.Off, TrayVisualStateResolver.Resolve(viewModel));

        viewModel.IsEnabled = true;
        Assert.Equal(TrayVisualState.Active, TrayVisualStateResolver.Resolve(viewModel));

        viewModel.DisableUntilTomorrow();
        Assert.Equal(TrayVisualState.SuspendedToday, TrayVisualStateResolver.Resolve(viewModel));
    }

    [Fact]
    public void HeaderStatusBrushReflectsVisualState()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 22, 30, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(clock: clock);

        Assert.Equal("#64748B", viewModel.HeaderStatusBrush);

        viewModel.IsEnabled = true;
        Assert.Equal("#16A34A", viewModel.HeaderStatusBrush);

        viewModel.DisableUntilTomorrow();
        Assert.Equal("#F59E0B", viewModel.HeaderStatusBrush);
    }

    [Fact]
    public async Task ChangingStartTimeWhileSleepingRestartsMonitoring()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 0, 40, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromHours(2), false)),
            new ClearContextDetector(),
            new FakeShutdownExecutor(),
            clock,
            action => action());

        viewModel.IsEnabled = true;
        await WaitUntilAsync(() => viewModel.StatusText == "Listo para 01:00");

        viewModel.StartTimeText = "00:40";

        await WaitUntilAsync(() => viewModel.IsCountdownActive);
        Assert.Equal("Apagado en 60 segundos", viewModel.StatusText);
    }

    [Fact]
    public async Task SleepingStatusShowsActualWakeTime()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 23, 0, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.Zero, false)),
            new ClearContextDetector(),
            new FakeShutdownExecutor(),
            clock,
            action => action())
        {
            StartTimeText = "00:15"
        };

        viewModel.IsEnabled = true;

        await WaitUntilAsync(() => viewModel.StatusText == "Durmiendo hasta 23:45");
    }

    [Fact]
    public async Task ScheduledCheckRestartsSleepingMonitorAndEvaluatesCurrentTime()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 0, 40, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromHours(2), false)),
            new ClearContextDetector(),
            new FakeShutdownExecutor(),
            clock,
            action => action());

        viewModel.IsEnabled = true;
        await WaitUntilAsync(() => viewModel.StatusText == "Listo para 01:00");

        clock.Now = new DateTimeOffset(2026, 4, 25, 1, 30, 0, TimeSpan.Zero);
        viewModel.RunScheduledCheck();

        await WaitUntilAsync(() => viewModel.IsCountdownActive);
        Assert.Equal("Apagado en 60 segundos", viewModel.StatusText);
    }

    [Fact]
    public async Task InvalidStartTimeDisarmsMonitoringInsteadOfUsingDefaultTime()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromHours(2), false)),
            new ClearContextDetector(),
            new FakeShutdownExecutor(),
            clock,
            action => action())
        {
            StartTimeText = "bad time"
        };

        viewModel.IsEnabled = true;

        await WaitUntilAsync(() => viewModel.StatusText == "Usa hora HH:mm");
        Assert.False(viewModel.CreateSettings().Enabled);
        Assert.False(viewModel.IsCountdownActive);
    }

    [Fact]
    public async Task IdleDetectorFailureBlocksShutdownWithoutPausingMonitor()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero));
        var shutdownExecutor = new FakeShutdownExecutor();
        var viewModel = new MainWindowViewModel(
            new ThrowingIdleDetector(),
            new ClearContextDetector(),
            shutdownExecutor,
            clock,
            action => action());

        viewModel.IsEnabled = true;

        await WaitUntilAsync(() => viewModel.StatusText == "Bloqueado: detector");
        Assert.False(viewModel.IsCountdownActive);
        Assert.Equal(0, shutdownExecutor.CallCount);
    }

    [Fact]
    public async Task PassiveStatusShowsIdleProgress()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromMinutes(12), false)),
            new ClearContextDetector(),
            new FakeShutdownExecutor(),
            clock,
            action => action());

        viewModel.IsEnabled = true;

        await WaitUntilAsync(() => viewModel.StatusText == "Inactivo 12/15 min");
        Assert.Equal("Smart Sleep Shutdown - ACTIVO - Inactivo 12/15 min", viewModel.TrayStatusText);
    }

    [Fact]
    public void ScheduleSummaryUpdatesWhenSettingsChange()
    {
        var viewModel = new MainWindowViewModel
        {
            StartTimeText = "03:10",
            IdleThresholdMinutes = 22,
            ContextChecksEnabled = false
        };

        Assert.Equal("Activo desde 03:10 hasta 06:00 | Inactividad 22 min | Contexto off", viewModel.ScheduleSummaryText);
    }

    [Fact]
    public async Task BlockingStatusShowsReason()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero));
        var viewModel = new MainWindowViewModel(
            new FakeIdleDetector(() => new IdleSnapshot(clock.Now, TimeSpan.FromMinutes(12), false)),
            new BlockedContextDetector("Audio is playing"),
            new FakeShutdownExecutor(),
            clock,
            action => action());

        viewModel.IsEnabled = true;

        await WaitUntilAsync(() => viewModel.StatusText == "Bloqueado: Audio is playing");
    }

    [Fact]
    public void ViewModelLoadsAndSavesUserSettings()
    {
        var store = new RecordingSettingsStore(new UserSettingsSnapshot(
            IsEnabled: true,
            StartTimeText: "02:15",
            IdleThresholdMinutes: 22,
            ContextChecksEnabled: false,
            TemporarilyDisabledUntil: null,
            ResumeAfterTemporaryDisable: false));

        var viewModel = new MainWindowViewModel(settingsStore: store);

        Assert.True(viewModel.IsEnabled);
        Assert.Equal("02:15", viewModel.StartTimeText);
        Assert.Equal(22, viewModel.IdleThresholdMinutes);
        Assert.False(viewModel.ContextChecksEnabled);

        viewModel.IdleThresholdMinutes = 30;

        Assert.NotNull(store.LastSaved);
        Assert.Equal(30, store.LastSaved.IdleThresholdMinutes);
    }

    [Fact]
    public void SettingsSaveFailureDoesNotCrashUiStateChange()
    {
        var viewModel = new MainWindowViewModel(settingsStore: new ThrowingSettingsStore());

        var exception = Record.Exception(() => viewModel.IsEnabled = true);

        Assert.Null(exception);
        Assert.True(viewModel.IsEnabled);
        Assert.Equal("No se pudo guardar la configuracion", viewModel.SettingsWarningText);
        Assert.True(viewModel.IsSettingsWarningVisible);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(predicate());
    }

    private sealed class FakeClock : ISystemClock
    {
        public FakeClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; set; }
    }

    private sealed class FakeIdleDetector : IIdleDetector
    {
        private readonly Func<IdleSnapshot> _snapshot;

        public FakeIdleDetector(Func<IdleSnapshot> snapshot)
        {
            _snapshot = snapshot;
        }

        public ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_snapshot());
        }
    }

    private sealed class ThrowingIdleDetector : IIdleDetector
    {
        public ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Idle detector failed.");
        }
    }

    private sealed class ClearContextDetector : IContextDetector
    {
        public ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(ContextSnapshot.Clear);
        }
    }

    private sealed class BlockedContextDetector : IContextDetector
    {
        private readonly string _reason;

        public BlockedContextDetector(string reason)
        {
            _reason = reason;
        }

        public ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(ContextSnapshot.Blocked(new BlockingContext(
                BlockingContextType.AudioPlaying,
                _reason)));
        }
    }

    private sealed class FakeShutdownExecutor : IShutdownExecutor
    {
        public int CallCount { get; private set; }

        public Task ShutdownNowAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSettingsStore : IUserSettingsStore
    {
        private readonly UserSettingsSnapshot? _loaded;

        public RecordingSettingsStore(UserSettingsSnapshot? loaded)
        {
            _loaded = loaded;
        }

        public UserSettingsSnapshot? LastSaved { get; private set; }

        public UserSettingsSnapshot? Load()
        {
            return _loaded;
        }

        public void Save(UserSettingsSnapshot snapshot)
        {
            LastSaved = snapshot;
        }
    }

    private sealed class ThrowingSettingsStore : IUserSettingsStore
    {
        public UserSettingsSnapshot? Load()
        {
            return null;
        }

        public void Save(UserSettingsSnapshot snapshot)
        {
            throw new IOException("settings write failed");
        }
    }
}
