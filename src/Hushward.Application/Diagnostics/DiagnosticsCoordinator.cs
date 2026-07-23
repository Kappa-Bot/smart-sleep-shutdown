using Hushward.Application.Abstractions;
using Hushward.Application.Configuration;
using Hushward.Application.Results;
using Hushward.Application.Runtime;

namespace Hushward.Application.Diagnostics;

public sealed class DiagnosticsCoordinator
{
    private readonly IHistoryStore _historyStore;

    public DiagnosticsCoordinator(IHistoryStore historyStore)
    {
        _historyStore = historyStore;
    }

    public async Task<OperationResult<DiagnosticSnapshot>> CreateSnapshotAsync(
        ConfigurationEnvelope configuration,
        string applicationVersion,
        string windowsVersion,
        string architecture,
        IReadOnlyDictionary<string, string> powerCapabilities,
        ScheduleHealth schedule,
        IReadOnlyList<DetectorHealth> detectorHealth,
        IReadOnlyList<DiagnosticError> errors,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        var events = await _historyStore.ReadRecentAsync(100, cancellationToken).ConfigureAwait(false);
        if (!events.IsSuccess)
        {
            return OperationResult<DiagnosticSnapshot>.Failure(
                events.Error!.Code,
                events.Error.MessageKey,
                events.Error.TechnicalDetail);
        }

        var snapshot = new DiagnosticSnapshot(
            generatedAt,
            applicationVersion,
            windowsVersion,
            architecture,
            new DiagnosticConfigurationSummary(
                configuration.SchemaVersion,
                configuration.Settings.Routines.Count,
                configuration.Settings.ProtectionRules.Count,
                configuration.Settings.Privacy.HistoryRetentionDays,
                configuration.Settings.InstallationState.StartWithWindows,
                configuration.Settings.InstallationState.WakeTasksEnabled,
                configuration.Settings.RequiresMigrationReview),
            powerCapabilities,
            schedule,
            events.Value!,
            detectorHealth,
            errors);

        return OperationResult<DiagnosticSnapshot>.Success(snapshot);
    }
}
