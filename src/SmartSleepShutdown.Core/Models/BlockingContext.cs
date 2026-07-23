namespace SmartSleepShutdown.Core.Models;

public sealed record BlockingContext(
    BlockingContextType Type,
    string Description,
    BlockingContextSeverity Severity = BlockingContextSeverity.Soft,
    BlockingContextCategory Category = BlockingContextCategory.General);
