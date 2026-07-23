namespace Hushward.Core.Protections;

public static class ProtectionPolicy
{
    public static ProtectionSummary Summarize(
        IReadOnlyCollection<ProtectionSignal> signals,
        DateTimeOffset now)
    {
        var critical = new List<ProtectionSignal>();
        var temporary = new List<ProtectionSignal>();
        var contextual = new List<ProtectionSignal>();
        var expired = new List<ProtectionSignal>();
        var inactive = new List<ProtectionSignal>();

        foreach (var signal in signals)
        {
            if (signal.State == ObservationState.Inactive)
            {
                inactive.Add(signal);
                continue;
            }

            if (signal.ExpiresAt is not null && signal.ExpiresAt < now)
            {
                expired.Add(signal);
                continue;
            }

            if (signal.State == ObservationState.Unknown || signal.Class == ProtectionClass.Critical)
            {
                critical.Add(signal);
                continue;
            }

            if (signal.Class == ProtectionClass.Temporary)
            {
                temporary.Add(signal);
                continue;
            }

            contextual.Add(signal);
        }

        return new ProtectionSummary(
            critical.AsReadOnly(),
            temporary.AsReadOnly(),
            contextual.AsReadOnly(),
            expired.AsReadOnly(),
            inactive.AsReadOnly());
    }
}
