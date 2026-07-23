using Hushward.Core.Routines;

namespace Hushward.Application.Configuration;

public sealed record HushwardConfiguration(
    IReadOnlyList<NightRoutine> Routines,
    TonightOverride? TonightOverride,
    IReadOnlyList<ProtectionRule> ProtectionRules,
    PrivacySettings Privacy,
    UiPreferences UiPreferences,
    InstallationState InstallationState,
    bool RequiresMigrationReview)
{
    public static HushwardConfiguration SafeMode() => new(
        [],
        null,
        [],
        new PrivacySettings(14),
        new UiPreferences(ReducedMotion: false),
        new InstallationState(StartWithWindows: false, WakeTasksEnabled: false),
        RequiresMigrationReview: true);
}

public sealed record ProtectionRule(
    string Category,
    string ProtectionClass,
    string FriendlyLabel);

public sealed record PrivacySettings(int HistoryRetentionDays);

public sealed record UiPreferences(bool ReducedMotion);

public sealed record InstallationState(
    bool StartWithWindows,
    bool WakeTasksEnabled);
