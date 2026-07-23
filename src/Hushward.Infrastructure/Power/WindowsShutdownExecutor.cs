using System.Diagnostics;
using Hushward.Core.Abstractions;

namespace Hushward.Infrastructure.Power;

public sealed class WindowsShutdownExecutor : IShutdownExecutor
{
    public async Task ShutdownNowAsync(CancellationToken cancellationToken)
    {
        var command = ShutdownCommand.CreateShutdownNow();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = command.FileName,
            Arguments = command.Arguments,
            UseShellExecute = command.UseShellExecute,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Could not start shutdown.exe.");

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"shutdown.exe exited with code {process.ExitCode}.");
        }
    }
}

