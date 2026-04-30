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
    public void ScheduledCheckIsBackgroundLaunch()
    {
        Assert.True(StartupIntent.IsScheduledCheck(["--scheduled-check"]));
        Assert.True(StartupIntent.IsBackgroundLaunch(["--scheduled-check"]));
    }

    [Fact]
    public void ScheduledCheckSignalsExistingPrimaryWithoutOpeningWindow()
    {
        Assert.True(StartupIntent.ShouldSignalScheduledCheck(["--scheduled-check"]));
        Assert.False(StartupIntent.ShouldActivateExistingPrimary(["--scheduled-check"]));
        Assert.False(StartupIntent.ShouldShowMainWindow(["--scheduled-check"]));
    }

    [Fact]
    public void NormalSecondLaunchActivatesExistingPrimaryWindow()
    {
        Assert.True(StartupIntent.ShouldActivateExistingPrimary([]));
    }
}
