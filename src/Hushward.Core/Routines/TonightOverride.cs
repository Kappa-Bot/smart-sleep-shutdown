using Hushward.Core.Actions;

namespace Hushward.Core.Routines;

public sealed record TonightOverride(
    Guid RoutineId,
    DateTimeOffset ExpiresAt,
    NightAction? Action,
    TimeOnly? Earliest,
    DateTimeOffset? PostponedUntil,
    bool PauseUntilTomorrow,
    bool DisableWake,
    bool RequireManualConfirmation);
