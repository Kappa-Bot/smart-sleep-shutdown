using Hushward.Infrastructure.Updates;

namespace Hushward.Infrastructure.Tests.Updates;

public sealed class VelopackUpdateServiceTests
{
    [Fact]
    public async Task InstallDownloadsCheckedUpdateBeforeApply()
    {
        var client = new RecordingVelopackClient { HasUpdate = true };
        var service = new VelopackUpdateService(client);

        var check = await service.CheckAsync(CancellationToken.None);
        var install = await service.InstallAsync(CancellationToken.None);

        Assert.True(check.IsSuccess);
        Assert.True(check.Value!.UpdateAvailable);
        Assert.True(install.IsSuccess);
        Assert.Equal(["check", "download", "apply"], client.Calls);
    }

    private sealed class RecordingVelopackClient : IVelopackClient
    {
        public bool HasUpdate { get; init; }
        public List<string> Calls { get; } = [];

        public bool IsInstalled => true;

        public Task<object?> CheckAsync(CancellationToken cancellationToken)
        {
            Calls.Add("check");
            return Task.FromResult<object?>(HasUpdate ? new object() : null);
        }

        public Task DownloadAsync(object update, CancellationToken cancellationToken)
        {
            Calls.Add("download");
            return Task.CompletedTask;
        }

        public void ApplyAndRestart(object update)
        {
            Calls.Add("apply");
        }
    }
}
