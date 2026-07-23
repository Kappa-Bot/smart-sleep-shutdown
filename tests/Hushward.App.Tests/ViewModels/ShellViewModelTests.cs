using Hushward.Application.Runtime;
using Hushward.App.ViewModels;
using Hushward.App.Runtime;
using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.App.Tests.ViewModels;

public sealed class ShellViewModelTests
{
    [Fact]
    public void ShellReceivesCanonicalRuntimeSnapshots()
    {
        var initial = NightRuntimeSnapshot.Empty(1, DateTimeOffset.Parse("2026-07-23T00:30:00Z"));
        var publisher = new RuntimeSnapshotPublisher(initial);
        using var shell = new ShellViewModel(new NightMonitorController(), publisher, action => action());
        var next = initial with
        {
            Sequence = 2,
            CapturedAt = initial.CapturedAt.AddMinutes(1),
            MonitoringState = RuntimeState.Monitoring
        };

        publisher.Publish(next);

        Assert.Equal(2, shell.Snapshot.Sequence);
        Assert.Equal(RuntimeState.Monitoring, shell.Snapshot.MonitoringState);
        Assert.Equal("Vigilando", shell.StatusText);

        publisher.Publish(next with
        {
            Sequence = 3,
            MonitoringState = RuntimeState.Protected
        });

        Assert.Equal("Bloqueado por actividad protegida", shell.StatusText);
    }

    [Fact]
    public async Task Applying_routine_persists_wake_choice_and_synchronizes_schedule()
    {
        NightRoutine? synchronized = null;
        var publisher = new RuntimeSnapshotPublisher(
            NightRuntimeSnapshot.Empty(1, DateTimeOffset.Parse("2026-07-23T00:30:00Z")));
        using var monitor = new NightMonitorController();
        using var shell = new ShellViewModel(
            monitor,
            publisher,
            action => action(),
            routine =>
            {
                synchronized = routine;
                return Task.CompletedTask;
            });
        var routine = new NightRoutine(
            Guid.NewGuid(),
            "Noche",
            true,
            Enum.GetValues<DayOfWeek>(),
            new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0)),
            TimeSpan.FromMinutes(15),
            NightAction.ShutDown,
            TimeSpan.FromSeconds(60),
            WakePolicy.WakeToEvaluate,
            LatestDecisionPolicy.KeepWaitingForProtections,
            []);

        await shell.ApplyRoutineAsync(routine);

        Assert.True(monitor.WakeEnabled);
        Assert.NotNull(synchronized);
        Assert.Equal(WakePolicy.WakeToEvaluate, synchronized.WakePolicy);

        shell.Tonight.PauseCommand.Execute(null);

        Assert.False(synchronized.Enabled);
    }
}
