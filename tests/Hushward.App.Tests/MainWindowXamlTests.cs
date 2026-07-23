namespace Hushward.App.Tests;

public sealed class MainWindowXamlTests
{
    [Fact]
    public void CountdownBindingIsOneWayBecauseViewModelPropertyIsReadOnly()
    {
        var xaml = ReadShell();

        Assert.Contains("CountdownSecondsRemaining, Mode=OneWay", xaml);
        Assert.DoesNotContain("Text=\"{Binding CountdownSecondsRemaining}\"", xaml);
    }

    [Fact]
    public void WindowExplainsThatCloseKeepsTrayIcon()
    {
        var xaml = ReadShell();

        Assert.Contains("loc:UiText.FooterHint", xaml);
        Assert.Contains("loc:UiText.AppTitle", xaml);
    }

    [Fact]
    public void WindowShowsClearSafetyAndTrayHints()
    {
        var xaml = ReadShell();

        Assert.Contains("loc:UiText.NoSilentShutdownHint", xaml);
        Assert.Contains("loc:UiText.ProtectionsHint", xaml);
    }

    [Fact]
    public void WindowCanShowSettingsSaveWarning()
    {
        var xaml = ReadShell();

        Assert.Contains("SettingsWarningText", xaml);
        Assert.Contains("IsSettingsWarningVisible", xaml);
    }

    [Fact]
    public void WindowShowsDynamicScheduleSummary()
    {
        var xaml = ReadShell();

        Assert.Contains("ScheduleSummaryText", xaml);
    }

    [Fact]
    public void WindowUsesBrandedIconAndDynamicStatusDot()
    {
        var xaml = ReadShell();

        Assert.Contains("Icon=\"{StaticResource AppIconImage}\"", xaml);
        Assert.Contains("Fill=\"{Binding HeaderStatusBrush}\"", xaml);
    }

    [Fact]
    public void SettingsTextBoxesCommitOnLostFocusToAvoidRestartingMonitorPerKeystroke()
    {
        var xaml = ReadShell();

        Assert.Contains("StartTimeText, UpdateSourceTrigger=LostFocus", xaml);
        Assert.Contains("IdleThresholdMinutes, UpdateSourceTrigger=LostFocus", xaml);
        Assert.DoesNotContain("StartTimeText, UpdateSourceTrigger=PropertyChanged", xaml);
        Assert.DoesNotContain("IdleThresholdMinutes, UpdateSourceTrigger=PropertyChanged", xaml);
    }

    [Fact]
    public void WindowUsesScrollViewerToPreventVerticalClipping()
    {
        var xaml = ReadShell();

        Assert.Contains("<ScrollViewer", xaml);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("CanContentScroll=\"False\"", xaml);
    }

    [Fact]
    public void FooterUsesGridLayoutToPreventTextButtonOverlap()
    {
        var xaml = ReadShell();

        Assert.Contains("x:Name=\"FooterLayout\"", xaml);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", xaml);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", xaml);
    }

    [Fact]
    public void WindowUsesPremiumMinimalVisualSystem()
    {
        var xaml = ReadShell();

        Assert.Contains("DeepNightBrush", xaml);
        Assert.Contains("HushwardToggleStyle", xaml);
        Assert.Contains("SurfaceCardStyle", xaml);
        Assert.Contains("StatusBadgeText", xaml);
        Assert.Contains("loc:UiText.ProtectionsHint", xaml);
    }

    private static string ReadShell() =>
        File.ReadAllText(FindProjectFile("src", "Hushward.App", "Views", "ShellWindow.xaml"));

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
