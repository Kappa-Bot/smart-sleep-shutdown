namespace SmartSleepShutdown.Core.Models;

public sealed record ContextSnapshot(
    bool HasBlockingContext,
    IReadOnlyList<BlockingContext> Blockers)
{
    public static ContextSnapshot Clear { get; } = new(false, Array.Empty<BlockingContext>());

    public static ContextSnapshot Blocked(params BlockingContext[] blockers)
    {
        return new ContextSnapshot(true, blockers);
    }
}
