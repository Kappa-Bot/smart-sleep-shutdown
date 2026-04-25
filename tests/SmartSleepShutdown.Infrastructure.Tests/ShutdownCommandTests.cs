using SmartSleepShutdown.Infrastructure.Power;

namespace SmartSleepShutdown.Infrastructure.Tests;

public sealed class ShutdownCommandTests
{
    [Fact]
    public void ShutdownNowCommandUsesFixedExecutableAndArguments()
    {
        var command = ShutdownCommand.CreateShutdownNow();

        Assert.EndsWith(@"System32\shutdown.exe", command.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/s /t 0", command.Arguments);
        Assert.False(command.UseShellExecute);
    }
}
