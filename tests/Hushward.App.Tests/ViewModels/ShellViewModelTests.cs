using Hushward.Application.Runtime;
using Hushward.App.ViewModels;

namespace Hushward.App.Tests.ViewModels;

public sealed class ShellViewModelTests
{
    [Fact]
    public void ShellReceivesCanonicalRuntimeSnapshots()
    {
        var initial = NightRuntimeSnapshot.Empty(1, DateTimeOffset.Parse("2026-07-23T00:30:00Z"));
        var publisher = new RuntimeSnapshotPublisher(initial);
        using var shell = new ShellViewModel(new MainWindowViewModel(), publisher, action => action());
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
}
