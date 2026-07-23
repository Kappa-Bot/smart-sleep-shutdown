using Hushward.Infrastructure.System;

namespace Hushward.Infrastructure.Tests;

public sealed class StartupRegistrationTests
{
    [Fact]
    public void BuildsQuotedRunCommandForInstalledExecutable()
    {
        var command = StartupRegistration.BuildRunCommand(@"C:\Users\me\AppData\Local\Hushward\Hushward.App.exe");

        Assert.Equal("\"C:\\Users\\me\\AppData\\Local\\Hushward\\Hushward.App.exe\" --startup", command);
    }
}

