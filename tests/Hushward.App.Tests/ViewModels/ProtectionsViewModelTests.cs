using Hushward.Application.Runtime;
using Hushward.App.ViewModels.Protections;
using Hushward.Core.Protections;

namespace Hushward.App.Tests.ViewModels;

public sealed class ProtectionsViewModelTests
{
    [Fact]
    public void UnknownRequiredEvidenceCannotBeDowngraded()
    {
        var now = DateTimeOffset.Now;
        var signal = ProtectionSignal.Unknown("audio", ProtectionCategory.Meeting, now, "detector.failure");
        var snapshot = NightRuntimeSnapshot.Empty(1, now) with
        {
            ProtectionSummary = ProtectionPolicy.Summarize([signal], now)
        };
        var viewModel = new ProtectionsViewModel(snapshot);

        Assert.True(viewModel.HasRequiredUnknownEvidence);
        Assert.False(viewModel.CanDisableRequiredProtection);
    }
}
