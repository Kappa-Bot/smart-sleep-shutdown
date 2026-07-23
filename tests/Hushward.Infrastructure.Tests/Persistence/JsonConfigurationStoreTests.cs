using Hushward.Application.Configuration;
using Hushward.Infrastructure.Persistence;
using Hushward.Infrastructure.Tests.TestSupport;

namespace Hushward.Infrastructure.Tests.Persistence;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public async Task Corrupt_live_file_restores_last_known_good_backup()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), "{not-json");
        await File.WriteAllTextAsync(temp.PathOf("config.backup.json"), TestJson.ValidEnvelope);
        var store = new JsonConfigurationStore(temp.Path);

        var result = await store.LoadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ConfigurationSource.Backup, result.Value!.Source);
        Assert.True(File.Exists(temp.PathOf("config.invalid.json")));
    }

    [Fact]
    public async Task Both_live_and_backup_corrupt_returns_recovery_required()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), "{not-json");
        await File.WriteAllTextAsync(temp.PathOf("config.backup.json"), "{also-not-json");
        var store = new JsonConfigurationStore(temp.Path);

        var result = await store.LoadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ConfigurationHealth.RecoveryRequired, result.Value!.Health);
        Assert.Empty(result.Value.Envelope.Settings.Routines);
    }

    [Fact]
    public async Task Future_schema_enters_recovery_required_instead_of_defaulting()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), TestJson.FutureEnvelope);
        var store = new JsonConfigurationStore(temp.Path);

        var result = await store.LoadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ConfigurationHealth.RecoveryRequired, result.Value!.Health);
        Assert.Equal(ConfigurationSource.SafeMode, result.Value.Source);
    }

    [Fact]
    public async Task Atomic_write_preserves_previous_live_as_backup()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), TestJson.ValidEnvelope);
        var store = new JsonConfigurationStore(temp.Path);

        var result = await store.WriteJsonAsync(TestJson.ValidEnvelopeVersionB, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"productVersion\": \"0.1.0-test\"", await File.ReadAllTextAsync(temp.PathOf("config.backup.json")));
        Assert.Contains("\"productVersion\": \"0.1.1-test\"", await File.ReadAllTextAsync(temp.PathOf("config.json")));
    }

    [Fact]
    public async Task Failed_post_write_verification_preserves_invalid_live_and_restores_backup()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), TestJson.ValidEnvelope);
        var store = new JsonConfigurationStore(
            temp.Path,
            (path, cancellationToken) =>
                Task.FromResult(path.EndsWith("config.json", StringComparison.Ordinal) ? "{corrupt-after-write" : File.ReadAllText(path)),
            AtomicFileWriter.WriteAsync);

        var result = await store.WriteJsonAsync(TestJson.ValidEnvelopeVersionB, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("\"productVersion\": \"0.1.1-test\"", await File.ReadAllTextAsync(temp.PathOf("config.invalid.json")));
        Assert.Contains("\"productVersion\": \"0.1.0-test\"", await File.ReadAllTextAsync(temp.PathOf("config.json")));
        Assert.Contains("\"productVersion\": \"0.1.0-test\"", await File.ReadAllTextAsync(temp.PathOf("config.backup.json")));
    }

    [Fact]
    public async Task Read_json_uses_recovery_path_instead_of_returning_corrupt_live()
    {
        using var temp = new TemporaryDirectory();
        await File.WriteAllTextAsync(temp.PathOf("config.json"), "{not-json");
        await File.WriteAllTextAsync(temp.PathOf("config.backup.json"), TestJson.ValidEnvelope);
        var store = new JsonConfigurationStore(temp.Path);

        var result = await store.ReadJsonAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"productVersion\": \"0.1.0-test\"", result.Value);
        Assert.True(File.Exists(temp.PathOf("config.invalid.json")));
        Assert.Contains("\"productVersion\": \"0.1.0-test\"", await File.ReadAllTextAsync(temp.PathOf("config.json")));
    }
}
