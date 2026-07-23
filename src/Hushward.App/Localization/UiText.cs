using System.Resources;

namespace Hushward.App.Localization;

public static class UiText
{
    private static readonly ResourceManager ResourceManager = new(
        "Hushward.App.Resources.Strings",
        typeof(UiText).Assembly);

    public static string AppTitle => Get(nameof(AppTitle));
    public static string AppSubtitle => Get(nameof(AppSubtitle));
    public static string PrimaryStatusLabel => Get(nameof(PrimaryStatusLabel));
    public static string NoSilentShutdownHint => Get(nameof(NoSilentShutdownHint));
    public static string StartTimeLabel => Get(nameof(StartTimeLabel));
    public static string StartTimeHint => Get(nameof(StartTimeHint));
    public static string IdleLabel => Get(nameof(IdleLabel));
    public static string IdleHint => Get(nameof(IdleHint));
    public static string ProtectionsLabel => Get(nameof(ProtectionsLabel));
    public static string ProtectionsHint => Get(nameof(ProtectionsHint));
    public static string WarningPrefix => Get(nameof(WarningPrefix));
    public static string WarningSuffix => Get(nameof(WarningSuffix));
    public static string Cancel => Get(nameof(Cancel));
    public static string PauseToday => Get(nameof(PauseToday));
    public static string FooterHint => Get(nameof(FooterHint));
    public static string NavHome => Get(nameof(NavHome));
    public static string NavTonight => Get(nameof(NavTonight));
    public static string NavRoutines => Get(nameof(NavRoutines));
    public static string NavProtections => Get(nameof(NavProtections));
    public static string EnabledText => Get(nameof(EnabledText));
    public static string DisabledText => Get(nameof(DisabledText));
    public static string EnableAutomationName => Get(nameof(EnableAutomationName));
    public static string ContextChecksLabel => Get(nameof(ContextChecksLabel));
    public static string TonightHint => Get(nameof(TonightHint));
    public static string RoutineHint => Get(nameof(RoutineHint));
    public static string StatusLiveRegionName => Get(nameof(StatusLiveRegionName));
    public static string StatusDisabled => Get(nameof(StatusDisabled));
    public static string StatusWatching => Get(nameof(StatusWatching));
    public static string StatusPausedTomorrow => Get(nameof(StatusPausedTomorrow));
    public static string StatusCancelledActivity => Get(nameof(StatusCancelledActivity));
    public static string StatusInvalidTime => Get(nameof(StatusInvalidTime));
    public static string StatusMonitoringPaused => Get(nameof(StatusMonitoringPaused));
    public static string StatusDetectorBlocked => Get(nameof(StatusDetectorBlocked));
    public static string StatusShuttingDown => Get(nameof(StatusShuttingDown));
    public static string StatusBlockedActivity => Get(nameof(StatusBlockedActivity));
    public static string StatusReadyToWarn => Get(nameof(StatusReadyToWarn));
    public static string StatusWaitingForWindow => Get(nameof(StatusWaitingForWindow));
    public static string StatusShutdownCountdown => Get(nameof(StatusShutdownCountdown));
    public static string StatusSleepingUntilFormat => Get(nameof(StatusSleepingUntilFormat));
    public static string StatusReadyForFormat => Get(nameof(StatusReadyForFormat));
    public static string StatusWaitingUntilFormat => Get(nameof(StatusWaitingUntilFormat));
    public static string StatusBlockedFormat => Get(nameof(StatusBlockedFormat));
    public static string StatusIdleProgressFormat => Get(nameof(StatusIdleProgressFormat));
    public static string ScheduleSummaryFormat => Get(nameof(ScheduleSummaryFormat));
    public static string ContextOn => Get(nameof(ContextOn));
    public static string ContextOff => Get(nameof(ContextOff));
    public static string SettingsSaveFailed => Get(nameof(SettingsSaveFailed));
    public static string TrayStatusDisabled => Get(nameof(TrayStatusDisabled));
    public static string TrayStatusPausedTomorrow => Get(nameof(TrayStatusPausedTomorrow));
    public static string TrayStatusActiveFormat => Get(nameof(TrayStatusActiveFormat));
    public static string TrayOpen => Get(nameof(TrayOpen));
    public static string TrayEnable => Get(nameof(TrayEnable));
    public static string TrayDisable => Get(nameof(TrayDisable));
    public static string TrayEnableNow => Get(nameof(TrayEnableNow));
    public static string TrayPauseTomorrow => Get(nameof(TrayPauseTomorrow));
    public static string TrayExit => Get(nameof(TrayExit));
    public static string TrayStillRunningTitle => Get(nameof(TrayStillRunningTitle));
    public static string TrayStillRunningMessage => Get(nameof(TrayStillRunningMessage));

    public static string Get(string key) =>
        ResourceManager.GetString(key) ?? throw new MissingManifestResourceException(key);
}
