using Hushward.Core.Actions;

namespace Hushward.Core.Routines;

public enum RoutineValidationCode
{
    EmptyName,
    EmptyDays,
    DuplicateDays,
    InvalidAction,
    MinimumIdleMustBePositive,
    WarningDurationOutOfRange,
    InvalidAlternative,
    DuplicateAlternative,
    OverlappingAlternative
}

public sealed record RoutineValidationError(
    RoutineValidationCode Code,
    string Message);

public static class RoutineValidation
{
    public static IReadOnlyList<RoutineValidationError> Validate(NightRoutine routine)
    {
        var errors = new List<RoutineValidationError>();

        if (string.IsNullOrWhiteSpace(routine.Name))
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.EmptyName, "Routine name is required."));
        }

        if (routine.Days.Count == 0)
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.EmptyDays, "At least one day is required."));
        }
        else if (routine.Days.Distinct().Count() != routine.Days.Count)
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.DuplicateDays, "Routine days must not be duplicated."));
        }

        if (routine.MinimumIdle <= TimeSpan.Zero)
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.MinimumIdleMustBePositive, "Minimum idle must be positive."));
        }

        if (!Enum.IsDefined(routine.PrimaryAction))
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.InvalidAction, "Primary action is invalid."));
        }
        else if (!IsWarningDurationValid(routine.PrimaryAction, routine.WarningDuration))
        {
            errors.Add(new RoutineValidationError(RoutineValidationCode.WarningDurationOutOfRange, "Warning duration is outside the allowed range."));
        }

        var seenAlternatives = new HashSet<AuthorizedAlternative>();
        var seenConditions = new HashSet<(NightAction Primary, string ConditionCode)>();
        foreach (var alternative in routine.AuthorizedAlternatives)
        {
            if (alternative.Primary != routine.PrimaryAction ||
                !Enum.IsDefined(alternative.Primary) ||
                !Enum.IsDefined(alternative.Alternative) ||
                alternative.Alternative == alternative.Primary ||
                string.IsNullOrWhiteSpace(alternative.ConditionCode))
            {
                errors.Add(new RoutineValidationError(RoutineValidationCode.InvalidAlternative, "Authorized alternative is invalid."));
            }

            if (!seenAlternatives.Add(alternative))
            {
                errors.Add(new RoutineValidationError(RoutineValidationCode.DuplicateAlternative, "Authorized alternative is duplicated."));
            }

            var conditionKey = (alternative.Primary, alternative.ConditionCode.Trim());
            if (!seenConditions.Add(conditionKey))
            {
                errors.Add(new RoutineValidationError(RoutineValidationCode.OverlappingAlternative, "Authorized alternative condition overlaps another alternative."));
            }
        }

        return errors;
    }

    public static bool IsWarningDurationValid(NightAction action, TimeSpan duration)
    {
        var (minimum, maximum) = GetWarningBounds(action);
        return minimum is not null &&
            duration >= minimum.Value &&
            duration <= maximum!.Value;
    }

    public static (TimeSpan? Minimum, TimeSpan? Maximum) GetWarningBounds(NightAction action)
    {
        return action switch
        {
            NightAction.ShutDown => (TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(300)),
            NightAction.Hibernate => (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(180)),
            NightAction.Sleep => (TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(120)),
            NightAction.Lock => (TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)),
            NightAction.WarnOnly => (TimeSpan.Zero, TimeSpan.Zero),
            _ => (null, null)
        };
    }
}
