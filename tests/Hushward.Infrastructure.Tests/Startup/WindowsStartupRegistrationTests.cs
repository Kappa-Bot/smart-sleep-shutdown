using Hushward.Infrastructure.Startup;

namespace Hushward.Infrastructure.Tests.Startup;

public sealed class WindowsStartupRegistrationTests
{
    [Fact]
    public async Task Enabled_registration_uses_stable_launcher_and_reads_back_healthy()
    {
        var registry = new MemoryRunKeyStore();
        var registration = new WindowsStartupRegistration(
            @"C:\Users\me\AppData\Local\Hushward\current\Hushward.App.exe",
            registry);

        var written = await registration.SetEnabledAsync(true, CancellationToken.None);
        var state = await registration.ReadAsync(CancellationToken.None);

        Assert.True(written.IsSuccess);
        Assert.True(state.IsSuccess);
        Assert.True(state.Value!.Enabled);
        Assert.True(state.Value.Healthy);
        Assert.Equal(
            "\"C:\\Users\\me\\AppData\\Local\\Hushward\\current\\Hushward.App.exe\" --startup",
            registry.Value);
    }

    [Fact]
    public async Task Disabled_registration_removes_only_product_value()
    {
        var registry = new MemoryRunKeyStore { Value = "\"old.exe\" --startup" };
        var registration = new WindowsStartupRegistration(@"C:\Hushward\Hushward.App.exe", registry);

        var result = await registration.SetEnabledAsync(false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(registry.Value);
    }

    private sealed class MemoryRunKeyStore : IRunKeyStore
    {
        public string? Value { get; set; }

        public string? Read() => Value;

        public void Write(string value) => Value = value;

        public void Delete() => Value = null;
    }
}
