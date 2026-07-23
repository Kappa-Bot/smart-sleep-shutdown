using System.Diagnostics;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class KnownProcessContextProbe : IContextProbe
{
    private static readonly KnownProcessRule[] BlockingProcessRules =
    [
        new("Teams", BlockingContextCategory.CallOrMeeting, "Teams activo"),
        new("ms-teams", BlockingContextCategory.CallOrMeeting, "Teams activo"),
        new("Zoom", BlockingContextCategory.CallOrMeeting, "Zoom activo"),
        new("obs64", BlockingContextCategory.RecordingOrStreaming, "OBS activo"),
        new("obs32", BlockingContextCategory.RecordingOrStreaming, "OBS activo"),
        new("steam", BlockingContextCategory.Gaming, "Steam activo"),
        new("devenv", BlockingContextCategory.Development, "Visual Studio activo"),
        new("Code", BlockingContextCategory.Development, "VS Code activo"),
        new("POWERPNT", BlockingContextCategory.Presentation, "PowerPoint activo")
    ];

    public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var processes = new ProcessCollection(Process.GetProcesses());
        var runningNames = processes.Names;

        foreach (var processName in runningNames)
        {
            var match = TryClassifyProcessName(processName);
            if (match is not null)
            {
                return ValueTask.FromResult<BlockingContext?>(new BlockingContext(
                    match.Value.Type,
                    match.Value.Description,
                    BlockingContextSeverity.Soft,
                    match.Value.Category));
            }
        }

        return ValueTask.FromResult<BlockingContext?>(null);
    }

    public static KnownProcessMatch? TryClassifyProcessName(string processName)
    {
        foreach (var rule in BlockingProcessRules)
        {
            if (string.Equals(processName, rule.ProcessName, StringComparison.OrdinalIgnoreCase))
            {
                return new KnownProcessMatch(
                    BlockingContextType.KnownProcess,
                    rule.Category,
                    rule.Description);
            }
        }

        return null;
    }

    public readonly record struct KnownProcessMatch(
        BlockingContextType Type,
        BlockingContextCategory Category,
        string Description);

    private readonly record struct KnownProcessRule(
        string ProcessName,
        BlockingContextCategory Category,
        string Description);

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
