using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Protections;
using Hushward.Core.Routines;
using Hushward.Core.Tests.TestSupport;
using Hushward.Core.Warnings;

namespace Hushward.Core.Tests.Decisions;

public sealed class NightPolicyEngineTests
{
    [Fact]
    public void Disabled_routine_never_warns_or_executes()
    {
        var input = NightEvaluationInputBuilder.Eligible() with
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid())
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.Disabled);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void Critical_protection_blocks_shutdown()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            Protections = new ProtectionSummary(
                Critical: [ProtectionSignal.Unknown("audio", ProtectionCategory.Media, DateTimeOffset.UtcNow, "detector.failure")],
                Temporary: [],
                Contextual: [],
                Expired: [],
                Inactive: [])
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.Protected);
        Assert.Null(result.AuthorizedAction);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.RequiredEvidenceUnknown);
    }

    [Fact]
    public void No_implicit_fallback_when_primary_is_unavailable()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.Hibernate) with
        {
            SupportedActions = new HashSet<NightAction> { NightAction.Sleep, NightAction.ShutDown }
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.CapabilityBlocked);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void Low_battery_uses_hibernate_only_when_explicitly_authorized()
    {
        var withoutAlternative = new NightEvaluationInputBuilder
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(60)
            }
        }
            .WithBattery(percent: 12, charging: false)
            .Build();

        Assert.Equal(
            NightAction.ShutDown,
            NightPolicyEngine.Evaluate(withoutAlternative).AuthorizedAction);

        var withAlternative = new NightEvaluationInputBuilder
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(60)
            }
        }
            .WithBattery(percent: 12, charging: false)
            .WithAuthorizedAlternative(
                NightAction.Hibernate,
                conditionCode: "battery-below-20")
            .Build();

        Assert.Equal(
            NightAction.Hibernate,
            NightPolicyEngine.Evaluate(withAlternative).AuthorizedAction);
    }

    [Fact]
    public void Required_unknown_evidence_degrades_warn_only_instead_of_blocking()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.WarnOnly) with
        {
            Protections = new ProtectionSummary(
                Critical: [ProtectionSignal.Unknown("audio", ProtectionCategory.Media, DateTimeOffset.UtcNow, "detector.failure")],
                Temporary: [],
                Contextual: [],
                Expired: [],
                Inactive: [])
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.Degraded);
        result.AuthorizedAction.ShouldBe(NightAction.WarnOnly);
    }

    [Fact]
    public void Temporary_protection_blocks_before_idle_warning()
    {
        var signal = new ProtectionSignal(
            "media",
            ProtectionCategory.Media,
            ProtectionClass.Temporary,
            ObservationState.Active,
            "media.active",
            "Protection.Media",
            DateTimeOffset.Parse("2026-07-23T01:29:00Z"),
            null,
            null);
        var input = NightEvaluationInputBuilder.Eligible(NightAction.Sleep) with
        {
            Protections = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T01:30:00Z"))
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.Protected);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.TemporaryProtectionActive);
    }

    [Fact]
    public void Contextual_signal_does_not_prevent_ready_to_warn()
    {
        var signal = new ProtectionSignal(
            "cpu",
            ProtectionCategory.ResourceWorkload,
            ProtectionClass.Contextual,
            ObservationState.Active,
            "cpu.moderate",
            "Protection.Cpu",
            DateTimeOffset.Parse("2026-07-23T01:29:00Z"),
            null,
            null);
        var input = NightEvaluationInputBuilder.Eligible(NightAction.Sleep) with
        {
            Protections = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T01:30:00Z"))
        };

        NightPolicyEngine.Evaluate(input).Kind.ShouldBe(NightDecisionKind.ReadyToWarn);
    }

    [Fact]
    public void Idle_below_threshold_waits()
    {
        var input = NightEvaluationInputBuilder.Eligible() with
        {
            IdleDuration = TimeSpan.FromMinutes(2)
        };

        NightPolicyEngine.Evaluate(input).Kind.ShouldBe(NightDecisionKind.WaitingForIdle);
    }

    [Fact]
    public void Manual_confirmation_override_blocks_automatic_warning()
    {
        var input = NightEvaluationInputBuilder.Eligible() with
        {
            TonightOverride = new TonightOverride(
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-07-23T06:00:00Z"),
                null,
                null,
                null,
                PauseUntilTomorrow: false,
                DisableWake: false,
                RequireManualConfirmation: true)
        };

        NightPolicyEngine.Evaluate(input).Kind.ShouldBe(NightDecisionKind.ManualConfirmationRequired);
    }

    [Fact]
    public void Active_warning_stays_warning_until_countdown_elapsed()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:29:30Z"))
        };

        NightPolicyEngine.Evaluate(input).Kind.ShouldBe(NightDecisionKind.WarningActive);
    }

    [Fact]
    public void Countdown_elapsed_authorizes_execution_after_fresh_check()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.CountdownElapsed(DateTimeOffset.Parse("2026-07-23T01:29:00Z"))
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.AuthorizedToExecute);
        result.AuthorizedAction.ShouldBe(NightAction.ShutDown);
    }

    [Fact]
    public void Input_during_warning_cancels_and_requires_fresh_idle()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.Active(DateTimeOffset.Parse("2026-07-23T01:29:30Z")),
            UserInputDetected = true
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.WaitingForIdle);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.WarningCancelledByInput);
    }

    [Fact]
    public void Cancelled_warning_rearms_after_fresh_idle_threshold_is_met()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.CancelledAwaitingFreshIdle(DateTimeOffset.Parse("2026-07-23T01:10:00Z")),
            IdleDuration = TimeSpan.FromMinutes(30)
        };

        NightPolicyEngine.Evaluate(input).Kind.ShouldBe(NightDecisionKind.ReadyToWarn);
    }

    [Fact]
    public void Cancelled_warning_waits_while_fresh_idle_threshold_is_not_met()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.CancelledAwaitingFreshIdle(DateTimeOffset.Parse("2026-07-23T01:10:00Z")),
            IdleDuration = TimeSpan.FromMinutes(2)
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.WaitingForIdle);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.WarningCancelledByInput);
    }

    [Fact]
    public void Ready_warning_uses_configured_routine_warning_duration()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(180)
            }
        };

        NightPolicyEngine.Evaluate(input).WarningDuration.ShouldBe(TimeSpan.FromSeconds(180));
    }

    [Fact]
    public void Latest_decision_warn_and_abandon_stops_automatic_action_for_the_night()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            LatestDecisionReached = true,
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(60),
                LatestDecisionPolicy = LatestDecisionPolicy.WarnAndAbandon
            }
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.AbandonedForNight);
        result.AuthorizedAction.ShouldBe(NightAction.WarnOnly);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.LatestDecisionReached);
    }

    [Fact]
    public void Latest_decision_uses_explicit_authorized_alternative()
    {
        var input = new NightEvaluationInputBuilder
        {
            LatestDecisionReached = true,
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(60),
                LatestDecisionPolicy = LatestDecisionPolicy.UseAuthorizedAlternative
            }
        }
            .WithAuthorizedAlternative(NightAction.Lock, "latest-decision")
            .Build();

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.ReadyToWarn);
        result.AuthorizedAction.ShouldBe(NightAction.Lock);
        result.PrimaryReason.ShouldBe(DecisionReasonCode.AuthorizedAlternativeSelected);
    }

    [Fact]
    public void Unsupported_authorized_alternative_blocks_without_fallback()
    {
        var input = new NightEvaluationInputBuilder
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
            {
                Enabled = true,
                PrimaryAction = NightAction.ShutDown,
                WarningDuration = TimeSpan.FromSeconds(60)
            },
            SupportedActions = new HashSet<NightAction> { NightAction.ShutDown }
        }
            .WithBattery(percent: 12, charging: false)
            .WithAuthorizedAlternative(NightAction.Hibernate, "battery-below-20")
            .Build();

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.CapabilityBlocked);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void Final_check_blocks_execution_when_fresh_protection_appears()
    {
        var signal = new ProtectionSignal(
            "meeting",
            ProtectionCategory.Meeting,
            ProtectionClass.Critical,
            ObservationState.Active,
            "meeting.active",
            "Protection.Meeting",
            DateTimeOffset.Parse("2026-07-23T01:30:00Z"),
            null,
            null);
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            WarningState = WarningState.CountdownElapsed(DateTimeOffset.Parse("2026-07-23T01:29:00Z")),
            Protections = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T01:30:00Z"))
        };

        var result = NightPolicyEngine.Evaluate(input);

        result.Kind.ShouldBe(NightDecisionKind.Protected);
        Assert.Null(result.AuthorizedAction);
    }
}
