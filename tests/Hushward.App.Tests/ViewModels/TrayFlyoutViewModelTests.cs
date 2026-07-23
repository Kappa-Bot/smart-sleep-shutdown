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
        using var viewModel = new TrayFlyoutViewModel(publisher, () => { }, () => { }, () => { });

        Assert.Equal(snapshot.PrimaryReason, viewModel.PrimaryReasonCode);
        Assert.Equal("Protegido", viewModel.StateLabel);
    }
}
