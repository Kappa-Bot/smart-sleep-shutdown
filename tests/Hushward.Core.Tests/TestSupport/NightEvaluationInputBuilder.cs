using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Protections;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.Core.Tests.TestSupport;

public sealed record NightEvaluationInputBuilder
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-07-23T01:30:00+00:00");

    public NightEvaluationInputBuilder()
    {
    }

    public NightRoutine Routine { get; init; } =
        NightRoutine.CreateDefault(Guid.Parse("22222222-2222-2222-2222-222222222222")) with
        {
            Enabled = true
        };

    public DateTimeOffset EvaluatedAt { get; init; } = FixedNow;

    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    public TimeSpan IdleDuration { get; init; } = TimeSpan.FromMinutes(30);

    public ProtectionSummary Protections { get; init; } = ProtectionPolicy.Summarize([], FixedNow);

    public IReadOnlySet<NightAction> SupportedActions { get; init; } =
        new HashSet<NightAction>(Enum.GetValues<NightAction>());

    public WarningState WarningState { get; init; } = WarningState.None;

    public bool UserInputDetected { get; init; }

    public bool InstallUpdateOrRecoveryActive { get; init; }

    public bool LatestDecisionReached { get; init; }

    public int? BatteryPercent { get; init; }

    public bool BatteryCharging { get; init; }

    public static NightEvaluationInput Eligible(NightAction action = NightAction.Hibernate)
    {
        var builder = new NightEvaluationInputBuilder
        {
            Routine = NightRoutine.CreateDefault(Guid.Parse("22222222-2222-2222-2222-222222222222")) with
            {
                Enabled = true,
                PrimaryAction = action,
                WarningDuration = Core.Warnings.WarningPolicy.DefaultFor(action)
            }
        };

        return builder.Build();
    }

    public NightEvaluationInputBuilder WithBattery(int percent, bool charging) => this with
    {
        BatteryPercent = percent,
        BatteryCharging = charging
    };

    public NightEvaluationInputBuilder WithAuthorizedAlternative(
        NightAction alternative,
        string conditionCode) => this with
        {
            Routine = Routine with
            {
                AuthorizedAlternatives =
                [
                    .. Routine.AuthorizedAlternatives,
                    new AuthorizedAlternative(Routine.PrimaryAction, alternative, conditionCode)
                ]
            }
        };

    public NightEvaluationInput Build() => new(
        EvaluatedAt,
        TimeZone,
        Routine,
        TonightOverride: null,
        IdleDuration,
        Protections,
        SupportedActions,
        WarningState,
        UserInputDetected,
        InstallUpdateOrRecoveryActive,
        LatestDecisionReached,
        BatteryPercent,
        BatteryCharging);
}
