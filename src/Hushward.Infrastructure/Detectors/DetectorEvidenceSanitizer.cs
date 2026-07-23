namespace Hushward.Infrastructure.Detectors;

public static class DetectorEvidenceSanitizer
{
    private static readonly string[] UnsafeExtensions =
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

    public static string? FriendlyLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        var trimmed = label.Trim();
        if (trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains(":\\", StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.StartsWith("-", StringComparison.Ordinal)
            || UnsafeExtensions.Any(extension => trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return trimmed.Length <= 80 ? trimmed : trimmed[..80];
    }
}
