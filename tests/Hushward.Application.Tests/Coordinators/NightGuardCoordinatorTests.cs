using Hushward.Application.Coordinators;
using Hushward.Application.Runtime;
using Hushward.Application.Tests.TestSupport;

namespace Hushward.Application.Tests.Coordinators;

public sealed class NightGuardCoordinatorTests
{
    [Fact]
    public async Task Concurrent_commits_publish_monotonic_sequences()
    {
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 1));
        var coordinator = new NightGuardCoordinator(publisher);

        await Task.WhenAll(
            coordinator.CommitAsync(RuntimeState.Monitoring, CancellationToken.None),
            coordinator.CommitAsync(RuntimeState.Protected, CancellationToken.None));

        Assert.Equal(3, publisher.Latest.Sequence);
    }

    [Fact]
    public async Task Final_evaluation_requires_expected_current_sequence()
    {
        var publisher = new RuntimeSnapshotPublisher(TestSnapshots.Create(sequence: 12));
        var coordinator = new NightGuardCoordinator(
            publisher,
            _ => throw new InvalidOperationException("stale final check should not evaluate"));

        var decision = await coordinator.EvaluateFinalAsync(11, CancellationToken.None);

        Assert.Equal(Core.Decisions.NightDecisionKind.Protected, decision.Kind);
        Assert.Equal(12, publisher.Latest.Sequence);
    }
}
