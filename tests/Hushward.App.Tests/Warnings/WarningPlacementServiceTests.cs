using System.Windows;
using Hushward.App.Warnings;

namespace Hushward.App.Tests.Warnings;

public sealed class WarningPlacementServiceTests
{
    [Fact]
    public void PlacesWarningInsideActiveMonitorWorkAreaAtAnyDpi()
    {
        var workArea = new System.Windows.Rect(1920, 0, 2560, 1440);

        var position = WarningPlacementService.Calculate(
            workArea,
            new System.Windows.Size(420, 260),
            dpiScale: 1.5);

        Assert.True(position.X >= workArea.Left);
        Assert.True(position.Y >= workArea.Top);
        Assert.True(position.X + 420 <= workArea.Right);
        Assert.True(position.Y + 260 <= workArea.Bottom);
    }
}
