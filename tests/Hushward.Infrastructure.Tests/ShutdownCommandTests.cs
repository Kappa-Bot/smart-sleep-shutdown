using Hushward.Infrastructure.Power;

namespace Hushward.Infrastructure.Tests;

public sealed class ShutdownCommandTests
{
    [Fact]
    public void ShutdownNowCommandUsesFixedExecutableAndArguments()
    {
        var command = ShutdownCommand.CreateShutdownNow();

        Assert.Equal("shutdown.exe", command.FileName);
        Assert.Equal("/s /t 0", command.Arguments);
        Assert.False(command.UseShellExecute);
    }
}
