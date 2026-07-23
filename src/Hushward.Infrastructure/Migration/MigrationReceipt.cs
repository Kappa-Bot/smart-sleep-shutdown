namespace Hushward.Infrastructure.Migration;

public sealed record MigrationReceipt(
    string SourceFolder,
    string SourceSettingsSha256,
    string? SourceExecutableVersion,
    DateTimeOffset MigratedAt,
    int TargetSchemaVersion,
    string NewConfigurationSha256,
    string OldStartupRegistrationState,
    string OldWakeTaskState,
    string CompletionState);
