using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Hushward.App.Tests.Localization;

public sealed partial class ResourceCompletenessTests
{
    [Fact]
    public void EveryReferencedUiKeyExistsInSpanishResources()
    {
        var appRoot = FindProjectDirectory("src", "Hushward.App");
        var resourcePath = Path.Combine(appRoot, "Resources", "Strings.resx");
        var resourceKeys = XDocument.Load(resourcePath)
            .Root!
            .Elements("data")
            .Select(element => element.Attribute("name")!.Value)
            .ToHashSet(StringComparer.Ordinal);

        var referencedKeys = Directory
            .EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(Path.Combine("Localization", "UiText.cs"), StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => UiTextReference().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(referencedKeys);
        Assert.Empty(referencedKeys.Except(resourceKeys, StringComparer.Ordinal));
    }

    [Fact]
    public void ShellUsesResourceBackedSpanishCopy()
    {
        var shellPath = Path.Combine(
            FindProjectDirectory("src", "Hushward.App"),
            "Views",
            "ShellWindow.xaml");
        var xaml = File.ReadAllText(shellPath);

        Assert.Contains("loc:UiText.NavHome", xaml, StringComparison.Ordinal);
        Assert.Contains("loc:UiText.NavTonight", xaml, StringComparison.Ordinal);
        Assert.Contains("loc:UiText.NavRoutines", xaml, StringComparison.Ordinal);
        Assert.Contains("loc:UiText.NavProtections", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Hushward\"", xaml, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"UiText\.([A-Za-z][A-Za-z0-9_]*)", RegexOptions.CultureInvariant)]
    private static partial Regex UiTextReference();

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
