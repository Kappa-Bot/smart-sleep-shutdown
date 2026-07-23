using System.Windows.Input;
using Hushward.App.Localization;
using Hushward.App.Presentation;
using Hushward.App.ViewModels;
using Hushward.Core.Routines;
using Hushward.Core.Simulation;

namespace Hushward.App.ViewModels.Routines;

public sealed record RoutineValidationViewResult(
    bool IsValid,
    IReadOnlyList<RoutineOverlapConflict> Conflicts,
    IReadOnlyList<RoutineValidationError> Errors);

public sealed class RoutinesViewModel : ObservableObject
{
    private readonly IReadOnlyList<NightRoutine> _routines;
    private readonly TimeZoneInfo _timeZone;
    private string _validationText = string.Empty;

    public RoutinesViewModel(IReadOnlyList<NightRoutine> routines, TimeZoneInfo timeZone)
    {
        _routines = Array.AsReadOnly(routines.ToArray());
        _timeZone = timeZone;
        ValidateCommand = new RelayCommand(() => Validate());
    }

    public IReadOnlyList<NightRoutine> Routines => _routines;
    public ICommand ValidateCommand { get; }
    public string ValidationText
    {
        get => _validationText;
        private set => SetProperty(ref _validationText, value);
    }

    public RoutineValidationViewResult Validate()
    {
        var conflicts = RoutineOverlapDetector.FindConflicts(_routines, _timeZone);
        var errors = _routines.SelectMany(routine => routine.Validate()).ToArray();
        var result = new RoutineValidationViewResult(conflicts.Count == 0 && errors.Length == 0, conflicts, errors);
        ValidationText = result.IsValid ? UiText.RoutineValid : UiText.RoutineInvalid;
        return result;
    }

    public NightSimulationResult Simulate(NightSimulationRequest request) =>
        NightPolicySimulator.Simulate(request);
}
