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

    [Fact]
    public void WindowShowsClearSafetyAndTrayHints()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("1. Avisa 60 segundos", xaml);
        Assert.Contains("2. Cancela con actividad", xaml);
        Assert.Contains("3. Revisa bloqueos", xaml);
        Assert.Contains("Icono verde activo", xaml);
    }

    [Fact]
    public void WindowCanShowSettingsSaveWarning()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("SettingsWarningText", xaml);
        Assert.Contains("IsSettingsWarningVisible", xaml);
    }

    [Fact]
    public void WindowShowsDynamicScheduleSummary()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("ScheduleSummaryText", xaml);
    }

    [Fact]
    public void WindowUsesBrandedIconAndDynamicStatusDot()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("Icon=\"{StaticResource AppIconImage}\"", xaml);
        Assert.Contains("Fill=\"{Binding HeaderStatusBrush}\"", xaml);
    }

    [Fact]
    public void SettingsTextBoxesCommitOnLostFocusToAvoidRestartingMonitorPerKeystroke()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("StartTimeText, UpdateSourceTrigger=LostFocus", xaml);
        Assert.Contains("IdleThresholdMinutes, UpdateSourceTrigger=LostFocus", xaml);
        Assert.DoesNotContain("StartTimeText, UpdateSourceTrigger=PropertyChanged", xaml);
        Assert.DoesNotContain("IdleThresholdMinutes, UpdateSourceTrigger=PropertyChanged", xaml);
    }

    [Fact]
    public void WindowUsesScrollViewerToPreventVerticalClipping()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("<ScrollViewer", xaml);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("CanContentScroll=\"False\"", xaml);
    }

    [Fact]
    public void FooterUsesGridLayoutToPreventTextButtonOverlap()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("x:Name=\"FooterLayout\"", xaml);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", xaml);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", xaml);
    }

    [Fact]
    public void WindowUsesPremiumMinimalVisualSystem()
    {
        var xaml = File.ReadAllText(FindProjectFile("src", "SmartSleepShutdown.App", "MainWindow.xaml"));

        Assert.Contains("HeaderGradientBrush", xaml);
        Assert.Contains("PrimaryToggleStyle", xaml);
        Assert.Contains("CardStyle", xaml);
        Assert.Contains("StatusBadgeText", xaml);
        Assert.Contains("Juego o pantalla completa no bloquea tras 1h sin actividad", xaml);
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
