using Hushward.Application.Diagnostics;
using Hushward.Application.History;
using Hushward.Application.Runtime;
using Hushward.Core.Protections;
using Hushward.Infrastructure.Diagnostics;

namespace Hushward.Infrastructure.Tests.Diagnostics;

public sealed class DiagnosticBundleWriterTests
{
    [Theory]
    [InlineData("C:\\Users\\Ana\\secret.txt")]
    [InlineData("https://example.test/private")]
    [InlineData("secret.docx")]
    [InlineData("--token abc123")]
    [InlineData("/token abc123")]
    public async Task Diagnostic_bundle_never_contains_sensitive_raw_values(string secret)
    {
        var bundle = await DiagnosticBundleWriter.WriteAsync(
            DiagnosticFixture.WithTechnicalDetail(secret),
            CancellationToken.None);

        Assert.DoesNotContain(secret, bundle.ManifestText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("detector.audio", bundle.ManifestText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Diagnostic_bundle_contains_only_configuration_summary()
    {
        var bundle = await DiagnosticBundleWriter.WriteAsync(
            DiagnosticFixture.WithTechnicalDetail("safe detail"),
            CancellationToken.None);

        Assert.Contains("\"routineCount\": 1", bundle.ManifestText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"routines\"", bundle.ManifestText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("secret.docx")]
    [InlineData("/token abc123")]
    public async Task Power_capability_values_are_normalized_not_exported_raw(string unsafeValue)
    {
        var bundle = await DiagnosticBundleWriter.WriteAsync(
            DiagnosticFixture.WithTechnicalDetail("safe detail", unsafeValue),
            CancellationToken.None);

        Assert.DoesNotContain(unsafeValue, bundle.ManifestText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"state\": \"reported\"", bundle.ManifestText, StringComparison.Ordinal);
    }

    private static class DiagnosticFixture
    {
        public static DiagnosticSnapshot WithTechnicalDetail(string technicalDetail, string wakeTimerState = "enabled") =>
            new(
                new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero),
                "0.1.0-test",
                "Windows 11",
                "x64",
                new DiagnosticConfigurationSummary(
                    2,
                    RoutineCount: 1,
                    ProtectionRuleCount: 3,
                    HistoryRetentionDays: 14,
                    StartWithWindows: true,
                    WakeTasksEnabled: false,
                    RequiresMigrationReview: false),
                new Dictionary<string, string> { ["wakeTimers"] = wakeTimerState },
                new ScheduleHealth(true, new DateTimeOffset(2026, 7, 24, 0, 30, 0, TimeSpan.Zero), null),
                [
                    HistoryEvent.Create(
                        new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero),
                        HistoryEventKind.WaitingReasonChanged,
                        "proteccion.activa",
                        "audio",
                        "Dormitorio")
                ],
                [new DetectorHealth("detector.audio", true, ProtectionCategory.Media, null)],
                [
                    new DiagnosticError(
                        "detector.failed",
                        DiagnosticSeverity.Blocking,
                        "Diagnostics.DetectorFailed",
                        technicalDetail,
                        new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 7, 23, 1, 0, 0, TimeSpan.Zero),
                        2,
                        RecoveryRequired: false)
                ]);
    }
}
