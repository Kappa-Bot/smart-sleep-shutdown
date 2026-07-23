using Hushward.Core.Actions;

namespace Hushward.Core.Routines;

public sealed record NightRoutine(
    Guid Id,
    string Name,
    bool Enabled,
    IReadOnlyList<DayOfWeek> Days,
    NightWindow Window,
    TimeSpan MinimumIdle,
    NightAction PrimaryAction,
    TimeSpan WarningDuration,
    WakePolicy WakePolicy,
    LatestDecisionPolicy LatestDecisionPolicy,
    IReadOnlyList<AuthorizedAlternative> AuthorizedAlternatives)
{
    private IReadOnlyList<DayOfWeek> _days = Array.AsReadOnly(Days.ToArray());
    private IReadOnlyList<AuthorizedAlternative> _authorizedAlternatives = Array.AsReadOnly(AuthorizedAlternatives.ToArray());

    public IReadOnlyList<DayOfWeek> Days
    {
        get => _days;
        init => _days = Array.AsReadOnly(value.ToArray());
    }

    public IReadOnlyList<AuthorizedAlternative> AuthorizedAlternatives
    {
        get => _authorizedAlternatives;
        init => _authorizedAlternatives = Array.AsReadOnly(value.ToArray());
    }

    public static NightRoutine CreateDefault(Guid id) => new(
        id,
        "Mi rutina nocturna",
        false,
        [
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday
        ],
        new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0)),
        TimeSpan.FromMinutes(20),
        NightAction.Hibernate,
        TimeSpan.FromSeconds(45),
        WakePolicy.NeverWake,
        LatestDecisionPolicy.KeepWaitingForProtections,
        []);

    public IReadOnlyList<RoutineValidationError> Validate() => RoutineValidation.Validate(this);
}
