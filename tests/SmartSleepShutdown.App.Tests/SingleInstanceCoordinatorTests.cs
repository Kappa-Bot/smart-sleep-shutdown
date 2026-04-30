using SmartSleepShutdown.App;

namespace SmartSleepShutdown.App.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void SecondCoordinatorForSameNamesIsNotPrimary()
    {
        var names = TestNames();

        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
    }

    [Fact]
    public async Task SecondaryCoordinatorSignalsPrimaryActivationListener()
    {
        var names = TestNames();
        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        primary.StartActivationListener(() => activated.TrySetResult());
        secondary.SignalPrimaryInstance();

        var completed = await Task.WhenAny(activated.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(activated.Task, completed);
    }

    [Fact]
    public async Task SecondaryCoordinatorSignalsPrimaryExitListener()
    {
        var names = TestNames();
        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        var exitRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        primary.StartExitListener(() => exitRequested.TrySetResult());
        secondary.SignalPrimaryExit();

        var completed = await Task.WhenAny(exitRequested.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(exitRequested.Task, completed);
    }

    [Fact]
    public async Task SecondaryCoordinatorSignalsPrimaryScheduledCheckListener()
    {
        var names = TestNames();
        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName, names.ScheduledCheckEventName);
        var scheduledCheck = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        primary.StartScheduledCheckListener(() => scheduledCheck.TrySetResult());
        secondary.SignalPrimaryScheduledCheck();

        var completed = await Task.WhenAny(scheduledCheck.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(scheduledCheck.Task, completed);
    }

    private static (string InstanceName, string ActivationEventName, string ExitEventName, string ScheduledCheckEventName) TestNames()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return (
            $"Local\\SmartSleepShutdownTest-{suffix}-Instance",
            $"Local\\SmartSleepShutdownTest-{suffix}-Activate",
            $"Local\\SmartSleepShutdownTest-{suffix}-Exit",
            $"Local\\SmartSleepShutdownTest-{suffix}-Check");
    }
}
