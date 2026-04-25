using SmartSleepShutdown.App;

namespace SmartSleepShutdown.App.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void SecondCoordinatorForSameNamesIsNotPrimary()
    {
        var names = TestNames();

        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
    }

    [Fact]
    public async Task SecondaryCoordinatorSignalsPrimaryActivationListener()
    {
        var names = TestNames();
        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);
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
        using var primary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);
        using var secondary = SingleInstanceCoordinator.Create(names.InstanceName, names.ActivationEventName, names.ExitEventName);
        var exitRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        primary.StartExitListener(() => exitRequested.TrySetResult());
        secondary.SignalPrimaryExit();

        var completed = await Task.WhenAny(exitRequested.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(exitRequested.Task, completed);
    }

    private static (string InstanceName, string ActivationEventName, string ExitEventName) TestNames()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return (
            $"Local\\SmartSleepShutdownTest-{suffix}-Instance",
            $"Local\\SmartSleepShutdownTest-{suffix}-Activate",
            $"Local\\SmartSleepShutdownTest-{suffix}-Exit");
    }
}
