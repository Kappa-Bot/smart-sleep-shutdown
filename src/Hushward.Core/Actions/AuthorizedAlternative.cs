namespace Hushward.Core.Actions;

public sealed record AuthorizedAlternative(
    NightAction Primary,
    NightAction Alternative,
    string ConditionCode);
