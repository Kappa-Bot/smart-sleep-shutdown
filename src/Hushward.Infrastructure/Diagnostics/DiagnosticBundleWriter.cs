using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hushward.Application.Diagnostics;

namespace Hushward.Infrastructure.Diagnostics;

public static partial class DiagnosticBundleWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Task<DiagnosticBundle> WriteAsync(
        DiagnosticSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dto = new
        {
            snapshot.GeneratedAt,
            ApplicationVersion = SafeCode(snapshot.ApplicationVersion),
            WindowsVersion = SafeDiagnosticText(snapshot.WindowsVersion),
            Architecture = SafeCode(snapshot.Architecture),
            snapshot.Configuration,
            PowerCapabilities = snapshot.PowerCapabilities.Select(capability => new
            {
                Code = SafeCode(capability.Key),
                State = NormalizeCapabilityState(capability.Value)
            }),
            snapshot.Schedule,
            RecentEvents = snapshot.RecentEvents.Select(historyEvent => new
            {
                historyEvent.OccurredAt,
                historyEvent.Kind,
                ReasonCode = SafeCode(historyEvent.ReasonCode),
                CategoryCode = SafeNullableCode(historyEvent.CategoryCode),
                historyEvent.FriendlyApplicationLabel,
                historyEvent.OccurrenceCount,
                historyEvent.LastOccurredAt
            }),
            DetectorHealth = snapshot.DetectorHealth.Select(detector => new
            {
                DetectorId = SafeCode(detector.DetectorId),
                detector.Healthy,
                detector.Category,
                LastErrorCode = SafeNullableCode(detector.LastErrorCode)
            }),
            Errors = snapshot.Errors.Select(error => new
            {
                Code = SafeCode(error.Code),
                error.Severity,
                SummaryKey = SafeCode(error.SummaryKey),
                TechnicalDetail = error.TechnicalDetail is null ? null : "[detalle-redactado]",
                error.FirstOccurredAt,
                error.LastOccurredAt,
                error.Count,
                error.RecoveryRequired
            })
        };

        var manifest = Redact(JsonSerializer.Serialize(dto, JsonOptions)) ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(manifest);
        return Task.FromResult(new DiagnosticBundle(
            $"hushward-diagnostics-{snapshot.GeneratedAt:yyyyMMdd-HHmmss}.json",
            manifest,
            bytes));
    }

    private static string? SafeNullableCode(string? value) => value is null ? null : SafeCode(value);

    private static string SafeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var trimmed = value.Trim();
        return CodeRegex().IsMatch(trimmed) ? trimmed[..Math.Min(trimmed.Length, 80)] : "redacted";
    }

    private static string SafeDiagnosticText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var redacted = Redact(value) ?? "unknown";
        return redacted.Length <= 120 ? redacted : redacted[..120];
    }

    private static string NormalizeCapabilityState(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "enabled" or "habilitado" or "supported" or "soportado" => "available",
            "false" or "disabled" or "deshabilitado" or "unsupported" or "no-soportado" => "unavailable",
            "unknown" or "desconocido" => "unknown",
            _ => "reported"
        };
    }

    private static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = WindowsPathRegex().Replace(value, "[ruta]");
        redacted = UrlRegex().Replace(redacted, "[url]");
        redacted = TokenRegex().Replace(redacted, "$1[secreto]");
        redacted = CommandArgumentRegex().Replace(redacted, "$1[argumento]");
        return redacted;
    }

    [GeneratedRegex(@"[A-Za-z]:\\[^\s""']+", RegexOptions.Compiled)]
    private static partial Regex WindowsPathRegex();

    [GeneratedRegex(@"https?://[^\s""']+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"(--(?:token|secret|password|key)\s+)[^\s""']+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"(--[A-Za-z0-9-]+\s+)[^\s""']+", RegexOptions.Compiled)]
    private static partial Regex CommandArgumentRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.-]+$", RegexOptions.Compiled)]
    private static partial Regex CodeRegex();
}
