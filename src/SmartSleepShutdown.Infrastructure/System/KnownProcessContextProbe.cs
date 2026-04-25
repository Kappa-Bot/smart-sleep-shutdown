using System.Diagnostics;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class KnownProcessContextProbe : IContextProbe
{
    private static readonly string[] BlockingProcessNames =
    [
        "Teams",
        "ms-teams",
        "Zoom",
        "obs64",
        "obs32",
        "steam",
        "devenv",
        "Code",
        "POWERPNT"
    ];

    public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var processes = new ProcessCollection(Process.GetProcesses());
        var runningNames = processes.Names;

        foreach (var processName in BlockingProcessNames)
        {
            if (runningNames.Contains(processName))
            {
                return ValueTask.FromResult<BlockingContext?>(new BlockingContext(
                    BlockingContextType.KnownProcess,
                    $"{processName} is running"));
            }
        }

        return ValueTask.FromResult<BlockingContext?>(null);
    }

    private sealed class ProcessCollection : IDisposable
    {
        private readonly Process[] _processes;
        private HashSet<string>? _names;

        public ProcessCollection(Process[] processes)
        {
            _processes = processes;
        }

        public HashSet<string> Names => _names ??= _processes
            .Select(process => process.ProcessName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public void Dispose()
        {
            foreach (var process in _processes)
            {
                process.Dispose();
            }
        }
    }
}
