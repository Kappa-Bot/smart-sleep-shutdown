using SmartSleepShutdown.App;

namespace SmartSleepShutdown.App.Tests;

public sealed class TrayMenuTextTests
{
    [Fact]
    public void DisableUntilTomorrowTextIsReadableAscii()
    {
        Assert.Equal("Desactivar hasta manana", TrayMenuText.DisableUntilTomorrow);
    }
}
