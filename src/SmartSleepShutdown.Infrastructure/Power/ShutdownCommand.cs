namespace SmartSleepShutdown.Infrastructure.Power;

public sealed record ShutdownCommand(
    string FileName,
    string Arguments,
    bool UseShellExecute)
{
    public static ShutdownCommand CreateShutdownNow()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var executable = Path.Combine(windowsDirectory, "System32", "shutdown.exe");
        return new ShutdownCommand(executable, "/s /t 0", UseShellExecute: false);
    }
}
