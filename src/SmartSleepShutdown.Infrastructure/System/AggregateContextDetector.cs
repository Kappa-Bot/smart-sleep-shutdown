using SmartSleepShutdown.Core.Abstractions;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class AggregateContextDetector : IContextDetector
{
    private readonly IReadOnlyList<IContextProbe> _probes;

    public AggregateContextDetector(IEnumerable<IContextProbe> probes)
    {
        _probes = probes.ToArray();
    }

    public async ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken)
    {
        var blockers = new List<BlockingContext>();

        foreach (var probe in _probes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var blocker = await probe.DetectAsync(cancellationToken).ConfigureAwait(false);
                if (blocker is not null)
                {
                    blockers.Add(blocker);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                blockers.Add(new BlockingContext(
                    BlockingContextType.DetectorFailure,
                    $"{probe.GetType().Name} failed"));
            }
        }

        return blockers.Count == 0 ? ContextSnapshot.Clear : new ContextSnapshot(true, blockers);
    }

    public static AggregateContextDetector CreateDefault()
    {
        return new AggregateContextDetector(new IContextProbe[]
        {
            new ForegroundFullscreenContextProbe(),
            new AudioPlayingContextProbe(),
            new CpuUsageContextProbe(),
            new KnownProcessContextProbe()
        });
    }
}
