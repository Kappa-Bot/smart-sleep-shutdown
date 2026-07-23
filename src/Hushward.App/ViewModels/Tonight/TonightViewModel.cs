using System.Windows.Input;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.App.ViewModels;
using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.App.ViewModels.Tonight;

public sealed class TonightViewModel : ObservableObject
{
    private readonly NightRoutine _routine;
    private readonly DateTimeOffset _now;
    private readonly Action<TonightOverride>? _applyOverride;
    private TonightOverride? _override;

    public TonightViewModel(
        NightRoutine routine,
        DateTimeOffset now,
        Action<TonightOverride>? applyOverride = null)
    {
        _routine = routine;
        _now = now;
        _applyOverride = applyOverride;
        PauseCommand = new RelayCommand(PauseTonight);
        PostponeCommand = new RelayCommand(() => Postpone(30));
        RequireConfirmationCommand = new RelayCommand(RequireManualConfirmation);
    }

    public TonightOverride? Override
    {
        get => _override;
        private set
        {
            if (SetProperty(ref _override, value))
            {
                OnPropertyChanged(nameof(ExpiryText));
                if (value is not null)
                {
                    _applyOverride?.Invoke(value);
                }
            }
        }
    }

    public ICommand PauseCommand { get; }
    public ICommand PostponeCommand { get; }
    public ICommand RequireConfirmationCommand { get; }

    public string PermanentPlan => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        UiText.PermanentPlanFormat,
        _routine.Window.Earliest,
        (int)_routine.MinimumIdle.TotalMinutes);

    public string ExpiryText => Override is null
        ? UiText.NoTonightOverride
        : string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            UiText.OverrideExpiryFormat,
            Override.ExpiresAt);

    public void PauseTonight() =>
        Override = CreateOverride(pause: true);

    public void Postpone(int minutes) =>
        Override = CreateOverride(postponedUntil: _now.AddMinutes(Math.Clamp(minutes, 15, 60)));

    public void RequireManualConfirmation() =>
        Override = CreateOverride(requireManualConfirmation: true);

    private TonightOverride CreateOverride(
        bool pause = false,
        DateTimeOffset? postponedUntil = null,
        bool requireManualConfirmation = false) => new(
        _routine.Id,
        NextSixAm(_now),
        Action: null,
        Earliest: null,
        PostponedUntil: postponedUntil,
        PauseUntilTomorrow: pause,
        DisableWake: false,
        RequireManualConfirmation: requireManualConfirmation);

    private static DateTimeOffset NextSixAm(DateTimeOffset now)
    {
        var sixAm = new DateTimeOffset(now.Date.AddHours(6), now.Offset);
        return now < sixAm ? sixAm : sixAm.AddDays(1);
    }
}
