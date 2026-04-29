using SmartSleepShutdown.App;

namespace SmartSleepShutdown.App.Tests;

public sealed class StartupIntentTests
{
    [Fact]
    public void StartupArgumentIsBackgroundLaunch()
    {
        Assert.True(StartupIntent.IsBackgroundLaunch(["--startup"]));
    }

    [Fact]
    public void BackgroundLaunchDoesNotActivateExistingPrimaryWindow()
    {
        Assert.False(StartupIntent.ShouldActivateExistingPrimary(["--startup"]));
    }

    [Fact]
    public void NormalSecondLaunchActivatesExistingPrimaryWindow()
    {
        Assert.True(StartupIntent.ShouldActivateExistingPrimary([]));
    }
}
