using Hushward.Application.Runtime;

namespace Hushward.Application.Tests.TestSupport;

public static class TestSnapshots
{
    public static NightRuntimeSnapshot Create(
        long sequence = 1,
        DateTimeOffset? capturedAt = null) =>
        NightRuntimeSnapshot.Empty(sequence, capturedAt ?? DateTimeOffset.Parse("2026-07-23T01:00:00Z"));
}
