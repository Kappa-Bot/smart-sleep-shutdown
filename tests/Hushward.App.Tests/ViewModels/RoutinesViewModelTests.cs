using Hushward.App.ViewModels.Routines;
using Hushward.Core.Routines;

namespace Hushward.App.Tests.ViewModels;

public sealed class RoutinesViewModelTests
{
    [Fact]
    public void OverlappingEnabledRoutinesCannotBeSaved()
    {
        var first = NightRoutine.CreateDefault(Guid.NewGuid()) with { Enabled = true };
        var second = first with { Id = Guid.NewGuid(), Name = "Otra rutina" };
        var viewModel = new RoutinesViewModel([first, second], TimeZoneInfo.Utc);

        var result = viewModel.Validate();

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Conflicts);
    }
}
