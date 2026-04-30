namespace SmartSleepShutdown.Infrastructure.Tests;

public sealed class InstallScriptTests
{
    [Fact]
    public void LocalInstallerRegistersWakeScheduledTask()
    {
        var script = File.ReadAllText(FindProjectFile("scripts", "Install-Local.ps1"));

        Assert.Contains("SmartSleepShutdown-NightWake", script);
        Assert.Contains("New-ScheduledTaskTrigger", script);
        Assert.Contains("00:30", script);
        Assert.Contains("New-ScheduledTaskSettingsSet", script);
        Assert.Contains("-WakeToRun", script);
        Assert.Contains("Register-ScheduledTask", script);
        Assert.Contains("--startup", script);
        Assert.Contains("--scheduled-check", script);
        Assert.Contains("Repetition.Interval", script);
        Assert.Contains("PT5M", script);
        Assert.Contains("Repetition.Duration", script);
        Assert.Contains("PT6H", script);
        Assert.Contains("-RunLevel Limited", script);
        Assert.DoesNotContain("LeastPrivilege", script);
        Assert.Contains("powercfg", script);
        Assert.Contains("RTCWAKE", script);
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
