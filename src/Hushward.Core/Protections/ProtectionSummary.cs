namespace Hushward.Core.Protections;

public sealed record ProtectionSummary(
    IReadOnlyList<ProtectionSignal> Critical,
    IReadOnlyList<ProtectionSignal> Temporary,
    IReadOnlyList<ProtectionSignal> Contextual,
    IReadOnlyList<ProtectionSignal> Expired,
    IReadOnlyList<ProtectionSignal> Inactive)
{
    public bool HasCriticalBlock => Critical.Count > 0;

    public bool HasTemporaryBlock => Temporary.Count > 0;
}
