namespace Hushward.App.Tests;

public sealed class TabTemplateTests
{
    [Fact]
    public void Navigation_template_presents_headers_not_page_content()
    {
        var root = FindRepositoryRoot();
        var controls = File.ReadAllText(
            Path.Combine(root, "src", "Hushward.App", "Design", "Controls.xaml"));

        Assert.Contains("ContentSource=\"Header\"", controls, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hushward.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Hushward repository root was not found.");
    }
}
