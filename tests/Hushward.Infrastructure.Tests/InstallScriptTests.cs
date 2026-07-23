namespace Hushward.Infrastructure.Tests;

public sealed class InstallScriptTests
{
    [Fact]
    public void LocalInstallerRegistersWakeScheduledTask()
    {
        var script = File.ReadAllText(FindProjectFile("scripts", "Install-Local.ps1"));

        Assert.Contains("--startup", script);
        Assert.Contains("managed by Hushward when a routine enables wake", script);
        Assert.DoesNotContain("powercfg", script);
        Assert.DoesNotContain("RTCWAKE", script);
        Assert.DoesNotContain("Register-ScheduledTask", script);
        Assert.DoesNotContain("00:30", script);
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
