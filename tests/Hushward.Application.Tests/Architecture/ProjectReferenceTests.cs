using System.Xml.Linq;

namespace Hushward.Application.Tests.Architecture;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Application_references_only_core()
    {
        var references = ProjectReferences("src", "Hushward.Application", "Hushward.Application.csproj");

        Assert.Equal(["..\\Hushward.Core\\Hushward.Core.csproj"], references);
    }

    [Fact]
    public void Core_has_no_project_references()
    {
        Assert.Empty(ProjectReferences("src", "Hushward.Core", "Hushward.Core.csproj"));
    }

    [Fact]
    public void Only_app_project_uses_wpf()
    {
        var projectFiles = Directory.GetFiles(RepositoryRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToArray();

        foreach (var projectFile in projectFiles)
        {
            var usesWpf = XDocument.Load(projectFile)
                .Descendants("UseWPF")
                .Any(element => element.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

            if (usesWpf)
            {
                Assert.EndsWith(Path.Combine("src", "Hushward.App", "Hushward.App.csproj"), projectFile);
            }
        }
    }

    private static string[] ProjectReferences(params string[] pathParts)
    {
        var projectPath = Path.Combine(new[] { RepositoryRoot() }.Concat(pathParts).ToArray());
        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Cast<string>()
            .OrderBy(value => value)
            .ToArray();
    }

    private static string RepositoryRoot()
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

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
