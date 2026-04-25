namespace SmartSleepShutdown.Core.Models;

public sealed record BlockingContext(
    BlockingContextType Type,
    string Description);
