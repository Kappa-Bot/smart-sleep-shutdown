using Hushward.Application.Runtime;
using Hushward.App.Runtime;
using Hushward.App.ViewModels.Warnings;
using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Warnings;

namespace Hushward.App.Tests.ViewModels;

public sealed class WarningViewModelTests
{
    [Fact]
    public async Task AnyUserInputCancelsWarningWithoutExecutingAction()
    {
        var now = DateTimeOffset.Now;
        var snapshot = NightRuntimeSnapshot.Empty(8, now) with
        {
            MonitoringState = RuntimeState.Warning,
            Decision = NightDecision.Ready(NightAction.ShutDown, DecisionReasonCode.Ready, TimeSpan.FromSeconds(60)),
            WarningState = WarningState.Active(now)
        };
        var controller = new RecordingWarningController();
        using var viewModel = new WarningViewModel(
            new RuntimeSnapshotPublisher(snapshot),
            controller,
            _ => { },
            () => { });

        await viewModel.HandleUserInputAsync();

        Assert.Equal(1, controller.InputInvalidations);
        Assert.Equal(0, controller.Starts);
    }

    private sealed class RecordingWarningController : IWarningSessionController
    {
        public int Starts { get; private set; }
        public int InputInvalidations { get; private set; }

        public Task StartAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            Starts++;
            return Task.CompletedTask;
        }

        public Task InvalidateForInputAsync()
        {
            InputInvalidations++;
            return Task.CompletedTask;
        }

        public Task InvalidateForProtectionAsync() => Task.CompletedTask;
    }
}
