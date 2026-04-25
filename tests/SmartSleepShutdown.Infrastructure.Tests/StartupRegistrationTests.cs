using SmartSleepShutdown.Infrastructure.System;

namespace SmartSleepShutdown.Infrastructure.Tests;

public sealed class StartupRegistrationTests
{
    [Fact]
    public void BuildsQuotedRunCommandForInstalledExecutable()
    {
        var command = StartupRegistration.BuildRunCommand(@"C:\Users\me\AppData\Local\SmartSleepShutdown\SmartSleepShutdown.exe");

        Assert.Equal("\"C:\\Users\\me\\AppData\\Local\\SmartSleepShutdown\\SmartSleepShutdown.exe\" --startup", command);
    }
}
