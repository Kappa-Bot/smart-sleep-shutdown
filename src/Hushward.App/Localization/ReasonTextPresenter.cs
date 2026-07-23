using Hushward.Core.Decisions;

namespace Hushward.App.Localization;

public static class ReasonTextPresenter
{
    public static string Present(DecisionReasonCode? reason) => reason switch
    {
        DecisionReasonCode.RoutineDisabled => UiText.ReasonRoutineDisabled,
        DecisionReasonCode.OutsideNightWindow or DecisionReasonCode.DayNotSelected => UiText.ReasonOutsideWindow,
        DecisionReasonCode.IdleThresholdNotMet => UiText.ReasonIdleNotMet,
        DecisionReasonCode.CriticalProtectionActive => UiText.ReasonCriticalProtection,
        DecisionReasonCode.TemporaryProtectionActive => UiText.ReasonTemporaryProtection,
        DecisionReasonCode.RequiredEvidenceUnknown or DecisionReasonCode.FinalCheckFailed => UiText.ReasonEvidenceUnknown,
        DecisionReasonCode.ActionUnsupported => UiText.ReasonActionUnsupported,
        DecisionReasonCode.ManualConfirmationRequired => UiText.ReasonManualConfirmation,
        DecisionReasonCode.WarningCancelledByInput => UiText.ReasonCancelledByInput,
        DecisionReasonCode.WarningCancelledByProtection => UiText.ReasonCancelledByProtection,
        DecisionReasonCode.OperationInProgress => UiText.ReasonOperationInProgress,
        _ => UiText.ReasonReady
    };
}
