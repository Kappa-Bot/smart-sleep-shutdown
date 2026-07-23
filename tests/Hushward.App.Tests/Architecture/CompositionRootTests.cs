namespace Hushward.App.Tests.Architecture;

public sealed class CompositionRootTests
{
    [Fact]
    public void ViewModelsDoNotReferenceInfrastructureNamespace()
    {
        var viewModels = Directory.GetFiles(
            FindProjectDirectory("src", "Hushward.App", "ViewModels"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var file in viewModels)
        {
            var text = File.ReadAllText(file);

            Assert.DoesNotContain("using Hushward.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new Windows", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WindowsAdaptersAreCreatedOnlyByApplicationCompositionRoot()
    {
        var appRoot = FindProjectDirectory("src", "Hushward.App");
        var appCode = File.ReadAllText(Path.Combine(appRoot, "App.xaml.cs"));
        var shellCode = File.ReadAllText(Path.Combine(appRoot, "Views", "ShellWindow.xaml.cs"));

        Assert.Contains("CreateShellWindow", appCode, StringComparison.Ordinal);
        Assert.Contains("new Win32IdleDetector", appCode, StringComparison.Ordinal);
        Assert.Contains("new CoordinatedShutdownExecutor", appCode, StringComparison.Ordinal);
        Assert.Contains("new WindowsNightActionExecutor", appCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Hushward.Infrastructure", shellCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new Win32IdleDetector", shellCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new WindowsNightActionExecutor", shellCode, StringComparison.Ordinal);
    }

    private static string FindProjectDirectory(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray());
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(Path.Combine(pathParts));
    }
}
