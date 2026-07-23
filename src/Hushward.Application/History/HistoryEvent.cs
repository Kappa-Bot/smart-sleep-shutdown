namespace Hushward.Application.History;

public sealed record HistoryEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    HistoryEventKind Kind,
    string ReasonCode,
    string? CategoryCode,
    string? FriendlyApplicationLabel,
    int OccurrenceCount,
    DateTimeOffset LastOccurredAt)
{
    private string? _friendlyApplicationLabel = SafeFriendlyLabel(FriendlyApplicationLabel);

    private static readonly string[] UnsafeFileExtensions =
    [
        ".txt",
        ".doc",
        ".docx",
        ".pdf",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".jpg",
        ".jpeg",
        ".png",
        ".zip"
    ];

    public string? FriendlyApplicationLabel
    {
        get => _friendlyApplicationLabel;
        init => _friendlyApplicationLabel = SafeFriendlyLabel(value);
    }

    public int OccurrenceCount { get; init; } = Math.Max(1, OccurrenceCount);

    public DateTimeOffset LastOccurredAt { get; init; } =
        LastOccurredAt < OccurredAt ? OccurredAt : LastOccurredAt;

    public static HistoryEvent Create(
        DateTimeOffset occurredAt,
        HistoryEventKind kind,
        string reasonCode,
        string? categoryCode = null,
        string? friendlyApplicationLabel = null) =>
        new(
            Guid.NewGuid(),
            occurredAt,
            kind,
            reasonCode,
            categoryCode,
            SafeFriendlyLabel(friendlyApplicationLabel),
            OccurrenceCount: 1,
            LastOccurredAt: occurredAt);

    public bool IsSemanticallySameAs(HistoryEvent other) =>
        Kind == other.Kind
        && string.Equals(ReasonCode, other.ReasonCode, StringComparison.Ordinal)
        && string.Equals(CategoryCode, other.CategoryCode, StringComparison.Ordinal)
        && string.Equals(FriendlyApplicationLabel, other.FriendlyApplicationLabel, StringComparison.Ordinal);

    public HistoryEvent CoalesceWith(HistoryEvent later) =>
        this with
        {
            OccurrenceCount = OccurrenceCount + Math.Max(1, later.OccurrenceCount),
            LastOccurredAt = later.LastOccurredAt > LastOccurredAt ? later.LastOccurredAt : LastOccurredAt
        };

    private static string? SafeFriendlyLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        var trimmed = label.Trim();
        if (trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains(":\\", StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.StartsWith("-", StringComparison.Ordinal)
            || trimmed.StartsWith("/", StringComparison.Ordinal)
            || UnsafeFileExtensions.Any(extension => trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return trimmed.Length <= 80 ? trimmed : trimmed[..80];
    }
}
