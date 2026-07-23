using Hushward.App.ViewModels.Tonight;
using Hushward.Core.Routines;

namespace Hushward.App.Tests.ViewModels;

public sealed class TonightViewModelTests
{
    [Fact]
    public void PauseIsExplicitAndExpiresNextMorning()
    {
        var now = new DateTimeOffset(2026, 7, 23, 22, 0, 0, TimeSpan.FromHours(2));
        var routine = NightRoutine.CreateDefault(Guid.NewGuid());
        var viewModel = new TonightViewModel(routine, now);

        viewModel.PauseTonight();

        Assert.True(viewModel.Override!.PauseUntilTomorrow);
        Assert.Equal(new DateTimeOffset(2026, 7, 24, 6, 0, 0, now.Offset), viewModel.Override.ExpiresAt);
    }
}
