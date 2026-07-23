namespace Hushward.Core.Protections;

public sealed record ProtectionSignal(
    string DetectorId,
    ProtectionCategory Category,
    ProtectionClass Class,
    ObservationState State,
    string ReasonCode,
    string ExplanationKey,
    DateTimeOffset ObservedAt,
    DateTimeOffset? ExpiresAt,
    string? FriendlyApplicationLabel)
{
    public static ProtectionSignal Unknown(
        string detectorId,
        ProtectionCategory category,
        DateTimeOffset observedAt,
        string reasonCode) => new(
            detectorId,
            category,
            ProtectionClass.Critical,
            ObservationState.Unknown,
            reasonCode,
            "Protection.Unknown",
            observedAt,
            null,
            null);
}
