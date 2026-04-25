namespace SmartSleepShutdown.App.Settings;

public sealed record UserSettingsSnapshot(
    bool IsEnabled,
    string StartTimeText,
    int IdleThresholdMinutes,
    bool ContextChecksEnabled,
    DateTimeOffset? TemporarilyDisabledUntil,
    bool ResumeAfterTemporaryDisable);
