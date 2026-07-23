using System.Windows.Input;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.Core.Actions;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.App.ViewModels.Onboarding;

public sealed class OnboardingViewModel : ObservableObject
{
    public sealed record ActionOption(NightAction Action, string Label);
    private readonly Func<NightRoutine, Task> _saveAsync;
    private readonly AsyncCommand _nextCommand;
    private readonly AsyncCommand _confirmCommand;
    private int _step = 1;
    private bool _isComplete;

    public OnboardingViewModel(Func<NightRoutine, Task> saveAsync)
    {
        _saveAsync = saveAsync;
        RoutineId = Guid.NewGuid();
        _nextCommand = new AsyncCommand(_ => NextAsync(), () => Step < 4);
        _confirmCommand = new AsyncCommand(_ => ConfirmAndEnableAsync(), () => Step == 4 && !IsComplete);
    }

    public Guid RoutineId { get; }
    public int Step
    {
        get => _step;
        private set
        {
            if (SetProperty(ref _step, value))
            {
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(IsPurposeStep));
                OnPropertyChanged(nameof(IsActionStep));
                OnPropertyChanged(nameof(IsScheduleStep));
                OnPropertyChanged(nameof(IsReviewStep));
                _nextCommand.RaiseCanExecuteChanged();
                _confirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsComplete
    {
        get => _isComplete;
        private set
        {
            if (SetProperty(ref _isComplete, value))
            {
                _confirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public NightAction Action { get; set; } = NightAction.ShutDown;
    public IReadOnlyList<ActionOption> ActionOptions { get; } =
    [
        new(NightAction.ShutDown, UiText.ActionShutdown),
        new(NightAction.Hibernate, UiText.ActionHibernate),
        new(NightAction.Sleep, UiText.ActionSleep),
        new(NightAction.Lock, UiText.ActionLock),
        new(NightAction.WarnOnly, UiText.ActionWarnOnly)
    ];
    public TimeOnly Earliest { get; set; } = new(1, 0);
    public TimeOnly Latest { get; set; } = new(6, 0);
    public int IdleMinutes { get; set; } = 20;
    public bool StartWithWindows { get; set; } = true;
    public bool WakeEnabled { get; set; }
    public bool IsPurposeStep => Step == 1;
    public bool IsActionStep => Step == 2;
    public bool IsScheduleStep => Step == 3;
    public bool IsReviewStep => Step == 4;
    public string StepTitle => Step switch
    {
        1 => UiText.OnboardingStepPurpose,
        2 => UiText.OnboardingStepAction,
        3 => UiText.OnboardingStepSchedule,
        _ => UiText.OnboardingStepReview
    };

    public string Summary => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        UiText.OnboardingSummaryFormat,
        ActionLabel(Action),
        Earliest,
        IdleMinutes);

    public ICommand NextCommand => _nextCommand;
    public ICommand ConfirmCommand => _confirmCommand;

    public async Task NextAsync()
    {
        await _saveAsync(CreateRoutine(enabled: false)).ConfigureAwait(false);
        Step = Math.Min(4, Step + 1);
    }

    public async Task ConfirmAndEnableAsync()
    {
        if (Step < 4)
        {
            return;
        }

        await _saveAsync(CreateRoutine(enabled: true)).ConfigureAwait(false);
        IsComplete = true;
    }

    private NightRoutine CreateRoutine(bool enabled) => new(
        RoutineId,
        UiText.DefaultRoutineName,
        enabled,
        Enum.GetValues<DayOfWeek>(),
        new NightWindow(Earliest, Latest),
        TimeSpan.FromMinutes(Math.Clamp(IdleMinutes, 1, 240)),
        Action,
        WarningPolicy.DefaultFor(Action),
        WakeEnabled ? WakePolicy.WakeToEvaluate : WakePolicy.NeverWake,
        LatestDecisionPolicy.KeepWaitingForProtections,
        []);

    private static string ActionLabel(NightAction action) => action switch
    {
        NightAction.ShutDown => UiText.ActionShutdown,
        NightAction.Hibernate => UiText.ActionHibernate,
        NightAction.Sleep => UiText.ActionSleep,
        NightAction.Lock => UiText.ActionLock,
        _ => UiText.ActionWarnOnly
    };
}
