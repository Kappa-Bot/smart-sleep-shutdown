using Hushward.Core.Actions;
using Hushward.Core.Routines;
using Hushward.Core.Tests.TestSupport;

namespace Hushward.Core.Tests.Routines;

public sealed class TonightOverrideResolverTests
{
    [Fact]
    public void Temporary_action_expires_without_mutating_routine()
    {
        var routine = TestRoutines.Enabled(NightAction.ShutDown);
        var temporary = new TonightOverride(
            routine.Id,
            DateTimeOffset.Parse("2026-07-24T06:00:00Z"),
            NightAction.Hibernate,
            null,
            null,
            PauseUntilTomorrow: false,
            DisableWake: false,
            RequireManualConfirmation: false);

        var effective = TonightOverrideResolver.Resolve(
            routine,
            temporary,
            DateTimeOffset.Parse("2026-07-24T01:00:00Z"));

        effective.Action.ShouldBe(NightAction.Hibernate);
        routine.PrimaryAction.ShouldBe(NightAction.ShutDown);
    }

    [Fact]
    public void Expired_override_resolves_to_routine_plan()
    {
        var routine = TestRoutines.Enabled(NightAction.ShutDown);
        var expired = new TonightOverride(
            routine.Id,
            DateTimeOffset.Parse("2026-07-23T06:00:00Z"),
            NightAction.Hibernate,
            null,
            null,
            PauseUntilTomorrow: false,
            DisableWake: false,
            RequireManualConfirmation: false);

        var effective = TonightOverrideResolver.Resolve(
            routine,
            expired,
            DateTimeOffset.Parse("2026-07-24T01:00:00Z"));

        effective.Action.ShouldBe(NightAction.ShutDown);
        Assert.Null(effective.ActiveOverride);
    }

    [Fact]
    public void Pause_until_tomorrow_disables_effective_plan_only()
    {
        var routine = TestRoutines.Enabled(NightAction.Sleep);
        var paused = new TonightOverride(
            routine.Id,
            DateTimeOffset.Parse("2026-07-24T06:00:00Z"),
            null,
            null,
            null,
            PauseUntilTomorrow: true,
            DisableWake: false,
            RequireManualConfirmation: false);

        var effective = TonightOverrideResolver.Resolve(routine, paused, DateTimeOffset.Parse("2026-07-24T01:00:00Z"));

        effective.Enabled.ShouldBeFalse();
        routine.Enabled.ShouldBeTrue();
    }
}
