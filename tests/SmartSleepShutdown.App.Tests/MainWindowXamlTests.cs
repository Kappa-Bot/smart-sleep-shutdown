namespace SmartSleepShutdown.App.Tests;

public sealed class MainWindowXamlTests
{
    [Fact]
    public void CountdownBindingIsOneWayBecauseViewModelPropertyIsReadOnly()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("CountdownSecondsRemaining, Mode=OneWay", xaml);
        Assert.DoesNotContain("Text=\"{Binding CountdownSecondsRemaining}\"", xaml);
    }

    [Fact]
    public void WindowExplainsThatCloseKeepsTrayIcon()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("Al cerrar, sigue activo junto al reloj", xaml);
        Assert.Contains("Smart Sleep Shutdown", xaml);
    }

    private static string FindProjectFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {Path.Combine(pathParts)} from {AppContext.BaseDirectory}.");
    }
}
