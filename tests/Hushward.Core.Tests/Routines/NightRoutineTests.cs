using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.Core.Tests.Routines;

public sealed class NightRoutineTests
{
    [Fact]
    public void Defaults_are_safe_and_disabled()
    {
        var routine = NightRoutine.CreateDefault(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        routine.Enabled.ShouldBeFalse();
        routine.PrimaryAction.ShouldBe(NightAction.Hibernate);
        routine.WakePolicy.ShouldBe(WakePolicy.NeverWake);
        Assert.Empty(routine.AuthorizedAlternatives);
        Assert.Empty(routine.Validate());
    }

    [Fact]
    public void Rejects_alternative_equal_to_primary_action()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives =
            [
                new AuthorizedAlternative(
                    NightAction.ShutDown,
                    NightAction.ShutDown,
                    "battery-low")
            ]
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.InvalidAlternative);
    }

    [Fact]
    public void Rejects_action_warning_outside_allowed_range()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.Sleep,
            WarningDuration = TimeSpan.FromSeconds(10)
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.WarningDurationOutOfRange);
    }

    [Fact]
    public void Rejects_empty_days()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Days = []
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.EmptyDays);
    }

    [Fact]
    public void Rejects_nonpositive_idle()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            MinimumIdle = TimeSpan.Zero
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.MinimumIdleMustBePositive);
    }

    [Fact]
    public void Rejects_duplicate_authorized_alternatives()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives =
            [
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.Hibernate, "battery-below-20"),
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.Hibernate, "battery-below-20")
            ]
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.DuplicateAlternative);
    }

    [Theory]
    [InlineData(NightAction.ShutDown, 59, false)]
    [InlineData(NightAction.ShutDown, 60, true)]
    [InlineData(NightAction.Hibernate, 45, true)]
    [InlineData(NightAction.Sleep, 121, false)]
    [InlineData(NightAction.Lock, 10, true)]
    [InlineData(NightAction.WarnOnly, 1, false)]
    public void Enforces_action_specific_warning_bounds(NightAction action, int seconds, bool valid)
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = action,
            WarningDuration = TimeSpan.FromSeconds(seconds)
        };

        Assert.Equal(
            valid,
            !routine.Validate().Any(error => error.Code == RoutineValidationCode.WarningDurationOutOfRange));
    }

    [Fact]
    public void Rejects_duplicate_days()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Monday]
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.DuplicateDays);
    }

    [Fact]
    public void Allows_explicit_authorized_alternative()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives =
            [
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.Hibernate, "battery-below-20")
            ]
        };

        Assert.DoesNotContain(
            routine.Validate(),
            error => error.Code is RoutineValidationCode.InvalidAlternative or RoutineValidationCode.DuplicateAlternative);
    }

    [Fact]
    public void Invalid_primary_action_returns_typed_error()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = (NightAction)999
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.InvalidAction);
    }

    [Fact]
    public void Allows_warn_only_as_explicit_alternative()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives =
            [
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.WarnOnly, "latest-decision")
            ]
        };

        Assert.DoesNotContain(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.InvalidAlternative);
    }

    [Fact]
    public void Rejects_overlapping_alternative_condition()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives =
            [
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.Hibernate, "battery-below-20"),
                new AuthorizedAlternative(NightAction.ShutDown, NightAction.Sleep, "battery-below-20")
            ]
        };

        Assert.Contains(
            routine.Validate(),
            error => error.Code == RoutineValidationCode.OverlappingAlternative);
    }

    [Fact]
    public void Defensively_copies_days()
    {
        var days = new[] { DayOfWeek.Monday };
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with { Days = days };

        days[0] = DayOfWeek.Friday;

        routine.Days.Single().ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public void Exposed_days_cannot_be_mutated_by_casting_to_array()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid());

        Assert.False(routine.Days is DayOfWeek[]);
    }
}
