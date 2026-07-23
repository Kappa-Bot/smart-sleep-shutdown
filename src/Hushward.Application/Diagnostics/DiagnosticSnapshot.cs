using Hushward.Application.History;
using Hushward.Application.Runtime;

namespace Hushward.Application.Diagnostics;

public sealed record DiagnosticSnapshot(
    DateTimeOffset GeneratedAt,
    string ApplicationVersion,
    string WindowsVersion,
    string Architecture,
    DiagnosticConfigurationSummary Configuration,
    IReadOnlyDictionary<string, string> PowerCapabilities,
    ScheduleHealth Schedule,
    IReadOnlyList<HistoryEvent> RecentEvents,
    IReadOnlyList<DetectorHealth> DetectorHealth,
    IReadOnlyList<DiagnosticError> Errors);

public sealed record DiagnosticConfigurationSummary(
    int SchemaVersion,
    int RoutineCount,
    int ProtectionRuleCount,
    int HistoryRetentionDays,
    bool StartWithWindows,
    bool WakeTasksEnabled,
    bool RequiresMigrationReview);

public sealed record DiagnosticError(
    string Code,
    DiagnosticSeverity Severity,
    string SummaryKey,
    string? TechnicalDetail,
    DateTimeOffset FirstOccurredAt,
    DateTimeOffset LastOccurredAt,
    int Count,
    bool RecoveryRequired);
