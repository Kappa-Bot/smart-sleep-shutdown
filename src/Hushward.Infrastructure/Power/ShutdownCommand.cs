namespace Hushward.Infrastructure.Power;

public sealed record ShutdownCommand(
    string FileName,
    string Arguments,
    bool UseShellExecute)
{
    public static ShutdownCommand CreateShutdownNow()
    {
        return new ShutdownCommand("shutdown.exe", "/s /t 0", UseShellExecute: false);
    }
}
