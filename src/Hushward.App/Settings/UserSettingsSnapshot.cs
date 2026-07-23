using Hushward.Core.Actions;

namespace Hushward.App.Settings;

public sealed record UserSettingsSnapshot(
    bool IsEnabled,
    string StartTimeText,
    int IdleThresholdMinutes,
    bool ContextChecksEnabled,
    DateTimeOffset? TemporarilyDisabledUntil,
    bool ResumeAfterTemporaryDisable,
    bool WakeEnabled = false,
    NightAction SelectedAction = NightAction.ShutDown);
