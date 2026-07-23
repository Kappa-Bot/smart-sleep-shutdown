using System.Text.Json;
using Hushward.Application.Abstractions;
using Hushward.Application.Configuration;
using Hushward.Application.Results;

namespace Hushward.Infrastructure.Persistence;

public enum ConfigurationSource
{
    Live,
    Backup,
    SafeMode
}

public sealed record LoadedConfiguration(
    ConfigurationEnvelope Envelope,
    ConfigurationSource Source,
    ConfigurationHealth Health);

public sealed class JsonConfigurationStore : IConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;
    private readonly Func<string, CancellationToken, Task<string>> _readAllTextAsync;
    private readonly Func<string, string, CancellationToken, Task> _writeAsync;

    public JsonConfigurationStore(string directory)
        : this(directory, File.ReadAllTextAsync, AtomicFileWriter.WriteAsync)
    {
    }

    internal JsonConfigurationStore(
        string directory,
        Func<string, CancellationToken, Task<string>> readAllTextAsync,
        Func<string, string, CancellationToken, Task> writeAsync)
    {
        _directory = directory;
        _readAllTextAsync = readAllTextAsync;
        _writeAsync = writeAsync;
    }

    private string LivePath => global::System.IO.Path.Combine(_directory, "config.json");

    private string BackupPath => global::System.IO.Path.Combine(_directory, "config.backup.json");

    private string InvalidPath => global::System.IO.Path.Combine(_directory, "config.invalid.json");

    public async Task<OperationResult<LoadedConfiguration>> LoadAsync(CancellationToken cancellationToken)
    {
        var live = await TryLoadAsync(LivePath, cancellationToken).ConfigureAwait(false);
        if (live is not null)
        {
            return OperationResult<LoadedConfiguration>.Success(new LoadedConfiguration(
                live,
                ConfigurationSource.Live,
                ConfigurationHealth.Healthy));
        }

        PreserveInvalidLive();

        var backup = await TryLoadAsync(BackupPath, cancellationToken).ConfigureAwait(false);
        if (backup is not null)
        {
            Directory.CreateDirectory(_directory);
            File.Copy(BackupPath, LivePath, overwrite: true);
            return OperationResult<LoadedConfiguration>.Success(new LoadedConfiguration(
                backup,
                ConfigurationSource.Backup,
                ConfigurationHealth.RestoredFromBackup));
        }

        return OperationResult<LoadedConfiguration>.Success(new LoadedConfiguration(
            ConfigurationEnvelope.SafeMode(DateTimeOffset.UtcNow),
            ConfigurationSource.SafeMode,
            ConfigurationHealth.RecoveryRequired));
    }

    public async Task<OperationResult<string?>> ReadJsonAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(LivePath) && !File.Exists(BackupPath))
        {
            return OperationResult<string?>.Success(null);
        }

        var loaded = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return OperationResult<string?>.Failure(
                loaded.Error!.Code,
                loaded.Error.MessageKey,
                loaded.Error.TechnicalDetail);
        }

        if (loaded.Value!.Source == ConfigurationSource.SafeMode)
        {
            return OperationResult<string?>.Failure(
                "configuration.recovery-required",
                "Configuration.RecoveryRequired");
        }

        var json = await _readAllTextAsync(LivePath, cancellationToken).ConfigureAwait(false);
        return OperationResult<string?>.Success(json);
    }

    public async Task<OperationResult<Unit>> WriteJsonAsync(string json, CancellationToken cancellationToken)
    {
        if (Parse(json) is null)
        {
            return OperationResult<Unit>.Failure("configuration.invalid", "Configuration.Invalid");
        }

        await _writeAsync(LivePath, json, cancellationToken).ConfigureAwait(false);
        if (await TryLoadAsync(LivePath, cancellationToken).ConfigureAwait(false) is not null)
        {
            return OperationResult<Unit>.Success(new Unit());
        }

        PreserveInvalidLive();
        if (File.Exists(BackupPath))
        {
            File.Copy(BackupPath, LivePath, overwrite: true);
        }

        return OperationResult<Unit>.Failure("configuration.verify-failed", "Configuration.VerifyFailed");
    }

    private async Task<ConfigurationEnvelope?> TryLoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await _readAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return Parse(json);
    }

    private static ConfigurationEnvelope? Parse(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ConfigurationEnvelope>(json, JsonOptions);
            return envelope?.IsSupported() == true ? envelope : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void PreserveInvalidLive()
    {
        if (!File.Exists(LivePath))
        {
            return;
        }

        Directory.CreateDirectory(_directory);
        File.Copy(LivePath, InvalidPath, overwrite: true);
    }
}
