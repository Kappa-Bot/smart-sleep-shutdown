using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hushward.Application.Configuration;
using Hushward.Core.Actions;
using Hushward.Core.Routines;
using Hushward.Core.Warnings;

namespace Hushward.Infrastructure.Migration;

public sealed record MigrationResult(
    HushwardConfiguration Configuration,
    MigrationReceipt Receipt);

public static class LegacyToHushwardMigrator
{
    public static Task<MigrationResult> MigrateAsync(
        LegacySettings legacy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startTime = TimeOnly.TryParse(legacy.StartTime, out var parsed)
            ? parsed
            : new TimeOnly(1, 0);
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            Enabled = false,
            PrimaryAction = NightAction.ShutDown,
            WarningDuration = WarningPolicy.DefaultFor(NightAction.ShutDown),
            Window = new NightWindow(startTime, new TimeOnly(6, 0)),
            MinimumIdle = TimeSpan.FromMinutes(Math.Max(1, legacy.IdleThresholdMinutes))
        };
        var configuration = new HushwardConfiguration(
            [routine],
            null,
            [],
            new PrivacySettings(14),
            new UiPreferences(ReducedMotion: false),
            new InstallationState(StartWithWindows: false, WakeTasksEnabled: false),
            RequiresMigrationReview: true);

        var sourceJson = JsonSerializer.Serialize(legacy);
        var targetJson = JsonSerializer.Serialize(configuration);
        var receipt = new MigrationReceipt(
            "SmartSleepShutdown",
            Sha256(sourceJson),
            null,
            DateTimeOffset.UtcNow,
            ConfigurationEnvelope.CurrentSchemaVersion,
            Sha256(targetJson),
            "unknown",
            "unknown",
            "completed");

        return Task.FromResult(new MigrationResult(configuration, receipt));
    }

    private static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
