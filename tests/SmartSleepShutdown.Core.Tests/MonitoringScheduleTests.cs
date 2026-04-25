using SmartSleepShutdown.Core.Models;
using SmartSleepShutdown.Core.Services;

namespace SmartSleepShutdown.Core.Tests;

public sealed class MonitoringScheduleTests
{
    [Fact]
    public void BeforePrecheckSleepsUntilZeroThirty()
    {
        var now = new DateTimeOffset(2026, 4, 25, 23, 0, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(SleepShutdownSettings.Default with { Enabled = true }, now);

        Assert.Equal(TimeSpan.FromMinutes(90), delay);
    }

    [Fact]
    public void DaytimeSleepsUntilNextZeroThirty()
    {
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(SleepShutdownSettings.Default with { Enabled = true }, now);

        Assert.Equal(TimeSpan.FromHours(12.5), delay);
    }

    [Fact]
    public void BetweenPrecheckAndStartSleepsUntilOne()
    {
        var now = new DateTimeOffset(2026, 4, 25, 0, 30, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(SleepShutdownSettings.Default with { Enabled = true }, now);

        Assert.Equal(TimeSpan.FromMinutes(30), delay);
    }

    [Fact]
    public void StartTimeBeforePrecheckSleepsUntilStartTime()
    {
        var settings = SleepShutdownSettings.Default with
        {
            Enabled = true,
            StartTime = new TimeOnly(0, 15)
        };
        var now = new DateTimeOffset(2026, 4, 25, 23, 0, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);

        Assert.Equal(TimeSpan.FromMinutes(45), delay);
    }

    [Fact]
    public void BeforeCustomPrecheckSleepsUntilThirtyMinutesBeforeStart()
    {
        var settings = SleepShutdownSettings.Default with
        {
            Enabled = true,
            StartTime = new TimeOnly(2, 0)
        };
        var now = new DateTimeOffset(2026, 4, 25, 23, 0, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);

        Assert.Equal(TimeSpan.FromHours(2.5), delay);
    }

    [Fact]
    public void BetweenCustomPrecheckAndStartSleepsUntilStart()
    {
        var settings = SleepShutdownSettings.Default with
        {
            Enabled = true,
            StartTime = new TimeOnly(2, 0)
        };
        var now = new DateTimeOffset(2026, 4, 25, 1, 45, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);

        Assert.Equal(TimeSpan.FromMinutes(15), delay);
    }

    [Fact]
    public void LateEveningStartEvaluatesBeforeMidnight()
    {
        var settings = SleepShutdownSettings.Default with
        {
            Enabled = true,
            StartTime = new TimeOnly(23, 0)
        };
        var now = new DateTimeOffset(2026, 4, 25, 23, 30, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void LateEveningStartKeepsEvaluatingAfterMidnightUntilStopTime()
    {
        var settings = SleepShutdownSettings.Default with
        {
            Enabled = true,
            StartTime = new TimeOnly(23, 0)
        };
        var now = new DateTimeOffset(2026, 4, 26, 1, 30, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(settings, now);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void AfterStartEvaluatesImmediately()
    {
        var now = new DateTimeOffset(2026, 4, 25, 1, 0, 0, TimeSpan.Zero);

        var delay = MonitoringSchedule.GetDelayBeforeNextEvaluation(SleepShutdownSettings.Default with { Enabled = true }, now);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ActiveDelayUsesLongSleepWhenUserIsClearlyAwake()
    {
        var delay = MonitoringSchedule.GetDelayAfterEvaluation(
            SleepShutdownSettings.Default,
            new IdleSnapshot(DateTimeOffset.Now, TimeSpan.FromMinutes(1), false),
            DecisionState.Monitoring);

        Assert.Equal(TimeSpan.FromMinutes(1), delay);
    }

    [Fact]
    public void ActiveDelayUsesFastPollNearIdleThreshold()
    {
        var delay = MonitoringSchedule.GetDelayAfterEvaluation(
            SleepShutdownSettings.Default,
            new IdleSnapshot(DateTimeOffset.Now, TimeSpan.FromMinutes(14), false),
            DecisionState.Monitoring);

        Assert.Equal(TimeSpan.FromSeconds(5), delay);
    }
}
