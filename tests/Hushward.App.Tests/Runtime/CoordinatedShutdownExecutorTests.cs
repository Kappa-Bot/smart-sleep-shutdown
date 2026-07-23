using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Application.Runtime;
using Hushward.App.Runtime;
using Hushward.App.ViewModels;
using Hushward.Core.Abstractions;
using Hushward.Core.Actions;
using Hushward.Core.Models;
using Hushward.Core.Warnings;

namespace Hushward.App.Tests.Runtime;

public sealed class CoordinatedShutdownExecutorTests
{
    [Fact]
    public async Task FreshBlockerPreventsActionExecution()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 30, 0, TimeSpan.Zero);
        var action = new RecordingActionExecutor();
        var executor = Create(
            now,
            action,
            ContextSnapshot.Blocked(new BlockingContext(BlockingContextType.DetectorFailure, "failure")));
        await executor.StartAsync(TimeSpan.FromSeconds(60), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ShutdownNowAsync(CancellationToken.None));

        Assert.Empty(action.Calls);
    }

    [Fact]
    public async Task FreshEligibleStateExecutesThroughActionCoordinatorOnce()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 30, 0, TimeSpan.Zero);
        var action = new RecordingActionExecutor();
        var executor = Create(now, action, ContextSnapshot.Clear);
        await executor.StartAsync(TimeSpan.FromSeconds(60), CancellationToken.None);

        await executor.ShutdownNowAsync(CancellationToken.None);

        Assert.Equal([NightAction.ShutDown], action.Calls);
    }

    [Fact]
    public async Task DisablingDuringWarningInvalidatesCanonicalWarning()
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 30, 0, TimeSpan.Zero);
        var clock = new FakeClock(now);
        var idle = new FakeIdleDetector(now);
        var context = new FakeContextDetector(ContextSnapshot.Clear);
        var snapshots = new RuntimeSnapshotPublisher(NightRuntimeSnapshot.Empty(0, now));
        MainWindowViewModel? viewModel = null;
        var warning = new CoordinatedShutdownExecutor(
            idle,
            context,
            clock,
            () => viewModel!.CreateSettings(),
            new RecordingActionExecutor(),
            snapshots);
        using var ownedViewModel = viewModel = new MainWindowViewModel(
            idle,
            context,
            warning,
            clock,
            action => action(),
            warningSession: warning,
            runtimeSnapshots: snapshots);

        viewModel.IsEnabled = true;
        await WaitUntilAsync(() => snapshots.Latest.WarningState.Kind == WarningStateKind.Active);

        viewModel.IsEnabled = false;
        await WaitUntilAsync(() => snapshots.Latest.WarningState.Kind != WarningStateKind.Active);

        Assert.Equal(RuntimeState.Disabled, snapshots.Latest.MonitoringState);
    }

    private static CoordinatedShutdownExecutor Create(
        DateTimeOffset now,
        RecordingActionExecutor action,
        ContextSnapshot context)
    {
        var clock = new FakeClock(now);
        return new CoordinatedShutdownExecutor(
            new FakeIdleDetector(now),
            new FakeContextDetector(context),
            clock,
            () => SleepShutdownSettings.Default with { Enabled = true },
            action,
            new RuntimeSnapshotPublisher(NightRuntimeSnapshot.Empty(0, now)));
    }

    private sealed class FakeClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset Now { get; } = now;
    }

    private sealed class FakeIdleDetector(DateTimeOffset now) : IIdleDetector
    {
        public ValueTask<IdleSnapshot> GetIdleSnapshotAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(new IdleSnapshot(now, TimeSpan.FromHours(2), InputDetected: false));
    }

    private sealed class FakeContextDetector(ContextSnapshot snapshot) : IContextDetector
    {
        public ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(snapshot);
    }

    private sealed class RecordingActionExecutor : INightActionExecutor
    {
        public List<NightAction> Calls { get; } = [];

        public Task<OperationResult<Unit>> ExecuteAsync(
            NightAction action,
            CancellationToken cancellationToken)
        {
            Calls.Add(action);
            return Task.FromResult(OperationResult<Unit>.Success(new Unit()));
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 100 && !predicate(); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.True(predicate());
    }
}
