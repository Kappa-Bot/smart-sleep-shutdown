using SmartSleepShutdown.Core.Models;
using SmartSleepShutdown.Core.Services;

namespace SmartSleepShutdown.Core.Tests;

public sealed class DecisionEngineTests
{
    private static readonly DateTimeOffset ActiveTime = new(2026, 4, 25, 1, 0, 0, TimeSpan.Zero);
    private static readonly SleepShutdownSettings EnabledSettings = SleepShutdownSettings.Default with { Enabled = true };

    [Fact]
    public void DefaultSettingsAreSafe()
    {
        SleepShutdownSettings.Default.Enabled.ShouldBeFalse();
        SleepShutdownSettings.Default.StartTime.ShouldBe(new TimeOnly(1, 0));
        SleepShutdownSettings.Default.IdleThreshold.ShouldBe(TimeSpan.FromMinutes(15));
        SleepShutdownSettings.Default.WarningDuration.ShouldBe(TimeSpan.FromSeconds(60));
        SleepShutdownSettings.Default.ContextChecksEnabled.ShouldBeTrue();
    }

    [Fact]
    public void DisabledSettingsNeverStartWarning()
    {
        var engine = new DecisionEngine();

        var result = engine.Evaluate(
            SleepShutdownSettings.Default,
            Idle(TimeSpan.FromHours(2)),
            ContextSnapshot.Clear,
            ActiveTime);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Disabled);
    }

    [Fact]
    public void BeforeStartTimeDoesNotStartWarning()
    {
        var engine = new DecisionEngine();
        var now = new DateTimeOffset(2026, 4, 25, 0, 59, 0, TimeSpan.Zero);

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2), now), ContextSnapshot.Clear, now);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void AfterStopTimeDoesNotStartWarning()
    {
        var engine = new DecisionEngine();
        var now = new DateTimeOffset(2026, 4, 25, 6, 0, 0, TimeSpan.Zero);

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2), now), ContextSnapshot.Clear, now);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void WarningExpiryAfterStopTimeCancelsInsteadOfShuttingDown()
    {
        var engine = new DecisionEngine();
        var warningStart = new DateTimeOffset(2026, 4, 25, 5, 59, 30, TimeSpan.Zero);
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2), warningStart), ContextSnapshot.Clear, warningStart);

        var result = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromHours(2), warningStart.AddSeconds(60)),
            ContextSnapshot.Clear,
            warningStart.AddSeconds(60));

        result.Action.ShouldBe(ShutdownDecisionAction.CancelWarning);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void CrossMidnightStartIsActiveAfterMidnight()
    {
        var engine = new DecisionEngine();
        var settings = EnabledSettings with { StartTime = new TimeOnly(23, 0) };
        var now = new DateTimeOffset(2026, 4, 26, 1, 30, 0, TimeSpan.Zero);

        var result = engine.Evaluate(settings, Idle(TimeSpan.FromHours(2), now), ContextSnapshot.Clear, now);

        result.Action.ShouldBe(ShutdownDecisionAction.StartWarning);
        result.State.ShouldBe(DecisionState.Warning);
    }

    [Fact]
    public void IdleExactlyAtThresholdDoesNotStartWarning()
    {
        var engine = new DecisionEngine();

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromMinutes(15)), ContextSnapshot.Clear, ActiveTime);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void IdlePastThresholdAfterStartTimeStartsWarning()
    {
        var engine = new DecisionEngine();

        var result = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromMinutes(15).Add(TimeSpan.FromMilliseconds(1))),
            ContextSnapshot.Clear,
            ActiveTime);

        result.Action.ShouldBe(ShutdownDecisionAction.StartWarning);
        result.State.ShouldBe(DecisionState.Warning);
        result.WarningStartedAt.ShouldBe(ActiveTime);
    }

    [Fact]
    public void BlockingContextPreventsWarning()
    {
        var engine = new DecisionEngine();
        var context = ContextSnapshot.Blocked(new BlockingContext(BlockingContextType.AudioPlaying, "Audio playing"));
        var now = ActiveTime.AddMinutes(16);

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromMinutes(16), now), context, now);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void InputDuringWarningCancelsAndRequiresRearm()
    {
        var engine = new DecisionEngine();
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2)), ContextSnapshot.Clear, ActiveTime);

        var result = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromSeconds(1), ActiveTime.AddSeconds(10), inputDetected: true),
            ContextSnapshot.Clear,
            ActiveTime.AddSeconds(10));

        result.Action.ShouldBe(ShutdownDecisionAction.CancelWarning);
        result.State.ShouldBe(DecisionState.CancelledAwaitingRearm);
    }

    [Fact]
    public void WarningExpiryWithClearFinalCheckRequestsShutdown()
    {
        var engine = new DecisionEngine();
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2)), ContextSnapshot.Clear, ActiveTime);

        var result = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromHours(2), ActiveTime.AddSeconds(60)),
            ContextSnapshot.Clear,
            ActiveTime.AddSeconds(60));

        result.Action.ShouldBe(ShutdownDecisionAction.ShutdownNow);
        result.State.ShouldBe(DecisionState.ShutdownIssued);
    }

    [Fact]
    public void WarningExpiryWithDetectorFailureCancelsShutdown()
    {
        var engine = new DecisionEngine();
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2)), ContextSnapshot.Clear, ActiveTime);
        var context = ContextSnapshot.Blocked(new BlockingContext(BlockingContextType.DetectorFailure, "Probe failed"));

        var result = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromHours(2), ActiveTime.AddSeconds(60)),
            context,
            ActiveTime.AddSeconds(60));

        result.Action.ShouldBe(ShutdownDecisionAction.CancelWarning);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void LongIdleOverridesSoftGameMenuContext()
    {
        var engine = new DecisionEngine();
        var context = ContextSnapshot.Blocked(
            new BlockingContext(BlockingContextType.FullScreenApp, "Game menu is fullscreen"),
            new BlockingContext(BlockingContextType.KnownProcess, "Steam is running"),
            new BlockingContext(BlockingContextType.AudioPlaying, "Menu music is playing"),
            new BlockingContext(BlockingContextType.HighCpu, "Game menu is using CPU"));
        var now = ActiveTime.AddHours(1);

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(1), now), context, now);

        result.Action.ShouldBe(ShutdownDecisionAction.StartWarning);
        result.State.ShouldBe(DecisionState.Warning);
    }

    [Fact]
    public void DetectorFailureStillBlocksAfterLongIdle()
    {
        var engine = new DecisionEngine();
        var context = ContextSnapshot.Blocked(new BlockingContext(BlockingContextType.DetectorFailure, "Probe failed"));
        var now = ActiveTime.AddHours(2);

        var result = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2), now), context, now);

        result.Action.ShouldBe(ShutdownDecisionAction.None);
        result.State.ShouldBe(DecisionState.Monitoring);
    }

    [Fact]
    public void TransientContextDuringWarningDoesNotRestartCountdown()
    {
        var engine = new DecisionEngine();
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2)), ContextSnapshot.Clear, ActiveTime);
        var transientContext = ContextSnapshot.Blocked(new BlockingContext(BlockingContextType.AudioPlaying, "Brief notification sound"));

        var duringWarning = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromHours(2), ActiveTime.AddSeconds(1)),
            transientContext,
            ActiveTime.AddSeconds(1));

        duringWarning.Action.ShouldBe(ShutdownDecisionAction.None);
        duringWarning.State.ShouldBe(DecisionState.Warning);
        duringWarning.WarningStartedAt.ShouldBe(ActiveTime);

        var finalCheck = engine.Evaluate(
            EnabledSettings,
            Idle(TimeSpan.FromHours(2), ActiveTime.AddSeconds(60)),
            ContextSnapshot.Clear,
            ActiveTime.AddSeconds(60));

        finalCheck.Action.ShouldBe(ShutdownDecisionAction.ShutdownNow);
        finalCheck.State.ShouldBe(DecisionState.ShutdownIssued);
    }

    [Fact]
    public void CancelledStateDoesNotRetriggerUntilIdleResets()
    {
        var engine = new DecisionEngine();
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2)), ContextSnapshot.Clear, ActiveTime);
        engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromSeconds(1), ActiveTime.AddSeconds(5), inputDetected: true), ContextSnapshot.Clear, ActiveTime.AddSeconds(5));

        var stillIdle = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromHours(2), ActiveTime.AddMinutes(1)), ContextSnapshot.Clear, ActiveTime.AddMinutes(1));
        stillIdle.Action.ShouldBe(ShutdownDecisionAction.None);
        stillIdle.State.ShouldBe(DecisionState.CancelledAwaitingRearm);

        var reset = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromSeconds(30), ActiveTime.AddMinutes(2), inputDetected: true), ContextSnapshot.Clear, ActiveTime.AddMinutes(2));
        reset.State.ShouldBe(DecisionState.Monitoring);

        var retrigger = engine.Evaluate(EnabledSettings, Idle(TimeSpan.FromMinutes(16), ActiveTime.AddMinutes(20)), ContextSnapshot.Clear, ActiveTime.AddMinutes(20));
        retrigger.Action.ShouldBe(ShutdownDecisionAction.StartWarning);
    }

    private static IdleSnapshot Idle(TimeSpan idleDuration)
    {
        return Idle(idleDuration, ActiveTime);
    }

    private static IdleSnapshot Idle(TimeSpan idleDuration, DateTimeOffset now, bool inputDetected = false)
    {
        return new IdleSnapshot(now, idleDuration, inputDetected);
    }
}
