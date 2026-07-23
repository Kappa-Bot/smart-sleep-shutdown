using Hushward.Application.Runtime;
using Hushward.App.ViewModels.Tray;
using Hushward.Core.Decisions;

namespace Hushward.App.Tests.ViewModels;

public sealed class TrayFlyoutViewModelTests
{
    [Fact]
    public void TrayStatusComesFromCanonicalSnapshot()
    {
        var snapshot = NightRuntimeSnapshot.Empty(4, DateTimeOffset.Now) with
        {
            MonitoringState = RuntimeState.Protected,
            PrimaryReason = DecisionReasonCode.CriticalProtectionActive
        };
        var publisher = new RuntimeSnapshotPublisher(snapshot);
        var toggles = 0;
        using var viewModel = new TrayFlyoutViewModel(
            publisher,
            () => { },
            () => toggles++,
            () => { },
            () => { });

        Assert.Equal(snapshot.PrimaryReason, viewModel.PrimaryReasonCode);
        Assert.Equal("Protegido", viewModel.StateLabel);
        Assert.Contains("0 min", viewModel.IdleText);

        viewModel.ToggleEnabledCommand.Execute(null);
        Assert.Equal(1, toggles);
    }

    [Fact]
    public void Paused_snapshot_has_distinct_state_and_reactivate_command()
    {
        var now = DateTimeOffset.Now;
        var snapshot = NightRuntimeSnapshot.Empty(4, now) with
        {
            LastMeaningfulEvent = new RuntimeEvent("status.paused-today", now, null)
        };
        var publisher = new RuntimeSnapshotPublisher(snapshot);
        using var viewModel = new TrayFlyoutViewModel(
            publisher,
            () => { },
            () => { },
            () => { },
            () => { });

        Assert.Equal("Hushward · PAUSADO hasta mañana", viewModel.StateLabel);
        Assert.Equal("Activar ahora", viewModel.ToggleEnabledText);
    }
}
