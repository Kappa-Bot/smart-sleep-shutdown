using Hushward.Application.Runtime;
using Hushward.App.ViewModels.Home;
using Hushward.Core.Decisions;

namespace Hushward.App.Tests.ViewModels;

public sealed class HomeViewModelTests
{
    [Fact]
    public void HomeExplainsProtectedSnapshotInSpanish()
    {
        var snapshot = NightRuntimeSnapshot.Empty(1, DateTimeOffset.Now) with
        {
            MonitoringState = RuntimeState.Protected,
            PrimaryReason = DecisionReasonCode.RequiredEvidenceUnknown
        };

        var viewModel = new HomeViewModel(snapshot);

        Assert.Equal("Protegido", viewModel.StateLabel);
        Assert.Equal("Falta una comprobación necesaria; por seguridad no actuamos.", viewModel.PrimaryReason);
    }
}
