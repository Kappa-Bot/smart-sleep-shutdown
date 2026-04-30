using SmartSleepShutdown.Infrastructure.System;

namespace SmartSleepShutdown.Infrastructure.Tests;

public sealed class SystemSleepBlockerTests
{
    [Fact]
    public void AwakeFlagsKeepSystemAwakeWithoutForcingDisplayOn()
    {
        Assert.Contains("ES_SYSTEM_REQUIRED", WindowsSystemSleepBlocker.ActiveFlagNames);
        Assert.Contains("ES_CONTINUOUS", WindowsSystemSleepBlocker.ActiveFlagNames);
        Assert.DoesNotContain("ES_DISPLAY_REQUIRED", WindowsSystemSleepBlocker.ActiveFlagNames);
    }
}
