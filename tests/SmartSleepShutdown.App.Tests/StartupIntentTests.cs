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

    [Fact]
    public void ScheduleDiagnosticsPrintsAndDoesNotShowWindow()
    {
        Assert.True(StartupIntent.IsScheduleDiagnosticsRequest(["--diagnose-schedule"]));
        Assert.False(StartupIntent.ShouldShowMainWindow(["--diagnose-schedule"]));
        Assert.False(StartupIntent.ShouldActivateExistingPrimary(["--diagnose-schedule"]));
    }

    [Fact]
    public void DumpDiagnosticsPrintsAndDoesNotShowWindow()
    {
        Assert.True(StartupIntent.IsDumpDiagnosticsRequest(["--dump-diagnostics"]));
        Assert.False(StartupIntent.ShouldShowMainWindow(["--dump-diagnostics"]));
        Assert.False(StartupIntent.ShouldActivateExistingPrimary(["--dump-diagnostics"]));
    }
}
