namespace Hushward.Core.Models;

public sealed record BlockingContext(
    BlockingContextType Type,
    string Description);

