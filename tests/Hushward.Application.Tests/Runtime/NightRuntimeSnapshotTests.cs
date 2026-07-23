using Hushward.Application.Tests.TestSupport;
using Hushward.Core.Decisions;
using Hushward.Core.Actions;
using Hushward.Application.Runtime;

namespace Hushward.Application.Tests.Runtime;

public sealed class NightRuntimeSnapshotTests
{
    [Fact]
    public void Newer_sequence_replaces_older_sequence()
    {
        var older = TestSnapshots.Create(sequence: 10);
        var newer = TestSnapshots.Create(sequence: 11);

        Assert.True(newer.IsNewerThan(older));
        Assert.False(older.IsNewerThan(newer));
    }

    [Fact]
    public void Stale_snapshot_cannot_authorize_execution()
    {
        var snapshot = TestSnapshots.Create(
            sequence: 10,
            capturedAt: DateTimeOffset.Parse("2026-07-23T01:00:00Z"));

        Assert.True(snapshot.IsStaleAt(
            DateTimeOffset.Parse("2026-07-23T01:01:00Z"),
            TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void Supporting_reasons_are_copied_on_capture()
    {
        var reasons = new List<DecisionReasonCode> { DecisionReasonCode.Ready };
        var snapshot = NightRuntimeSnapshot.Empty(1, DateTimeOffset.Parse("2026-07-23T01:00:00Z")) with
        {
            SupportingReasons = reasons
        };

        reasons.Add(DecisionReasonCode.FinalCheckFailed);

        Assert.Single(snapshot.SupportingReasons);
    }

    [Fact]
    public void Supported_actions_are_copied_on_capture()
    {
        var actions = new HashSet<NightAction> { NightAction.ShutDown };
        var power = new PowerRuntimeState(
            IsOnBattery: false,
            BatteryPercent: null,
            IsCharging: false,
            actions);

        actions.Add(NightAction.Sleep);

        Assert.Single(power.SupportedActions);
    }
}
