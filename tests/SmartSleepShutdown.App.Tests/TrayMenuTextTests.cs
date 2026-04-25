using SmartSleepShutdown.App;

namespace SmartSleepShutdown.App.Tests;

public sealed class TrayMenuTextTests
{
    [Fact]
    public void DisableUntilTomorrowTextIsReadableAscii()
    {
        Assert.Equal("Desactivar hasta manana", TrayMenuText.DisableUntilTomorrow);
    }

    [Fact]
    public void TrayHintExplainsCloseToTray()
    {
        Assert.Equal("Sigue activo junto al reloj", TrayMenuText.StillRunningTitle);
        Assert.Contains("icono", TrayMenuText.StillRunningMessage);
    }
}
