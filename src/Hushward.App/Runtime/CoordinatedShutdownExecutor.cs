using Hushward.Application.Abstractions;
using Hushward.Application.Coordinators;
using Hushward.Application.Runtime;
using Hushward.Core.Abstractions;
using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Models;
using Hushward.Core.Services;
using Hushward.Core.Warnings;

namespace Hushward.App.Runtime;

public sealed class CoordinatedShutdownExecutor : IShutdownExecutor, IWarningSessionController
{
    private static readonly TimeSpan MaximumAuthorizationAge = TimeSpan.FromSeconds(5);

    private readonly IIdleDetector _idleDetector;
    private readonly IContextDetector _contextDetector;
    private readonly ISystemClock _clock;
    private readonly Func<SleepShutdownSettings> _settingsProvider;
    private readonly RuntimeSnapshotPublisher _publisher;
    private readonly Func<NightAction> _actionProvider;
    private readonly WarningCoordinator _warningCoordinator;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NightDecision? _freshFinalDecision;
    private DateTimeOffset? _freshFinalDecisionAt;

    public CoordinatedShutdownExecutor(
        IIdleDetector idleDetector,
        IContextDetector contextDetector,
        ISystemClock clock,
        Func<SleepShutdownSettings> settingsProvider,
        INightActionExecutor actionExecutor,
        RuntimeSnapshotPublisher publisher,
        Func<NightAction>? actionProvider = null)
    {
        _idleDetector = idleDetector;
        _contextDetector = contextDetector;
        _clock = clock;
        _settingsProvider = settingsProvider;
        _publisher = publisher;
        _actionProvider = actionProvider ?? (() => NightAction.ShutDown);

        var actionCoordinator = new ActionCoordinator(actionExecutor);
        var nightGuardCoordinator = new NightGuardCoordinator(publisher, EvaluateFinalSnapshot);
        _warningCoordinator = new WarningCoordinator(publisher, actionCoordinator, nightGuardCoordinator);
    }

    public async Task ShutdownNowAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _clock.Now;
            var settings = _settingsProvider();
            var idle = await _idleDetector.GetIdleSnapshotAsync(cancellationToken).ConfigureAwait(false);
            var context = settings.ContextChecksEnabled
                ? await _contextDetector.GetCurrentContextAsync(cancellationToken).ConfigureAwait(false)
                : ContextSnapshot.Clear;
            _freshFinalDecision = EvaluateFreshAuthorization(settings, idle, context, now);
            _freshFinalDecisionAt = now;
            var sequence = _publisher.Latest.Sequence;

            var result = await _warningCoordinator
                .CompleteCountdownAsync(sequence, _actionProvider(), cancellationToken)
                .ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Error!.Code);
            }
        }
        finally
        {
            _freshFinalDecision = null;
            _freshFinalDecisionAt = null;
            _gate.Release();
        }
    }

    public Task StartAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _warningCoordinator.StartAsync(_actionProvider(), duration, _clock.Now);
    }

    public Task InvalidateForInputAsync() =>
        _warningCoordinator.InvalidateAsync(
            new Application.Warnings.WarningInvalidation(
                Application.Warnings.WarningInvalidationKind.UserInput,
                "input.resumed"));

    public Task InvalidateForProtectionAsync() =>
        _warningCoordinator.InvalidateAsync(
            new Application.Warnings.WarningInvalidation(
                Application.Warnings.WarningInvalidationKind.ProtectionActivated,
                "protection.activated"));

    private NightDecision EvaluateFinalSnapshot(NightRuntimeSnapshot snapshot)
    {
        if (_freshFinalDecisionAt is null ||
            _clock.Now - _freshFinalDecisionAt > MaximumAuthorizationAge ||
            _freshFinalDecision?.Kind != NightDecisionKind.AuthorizedToExecute ||
            _freshFinalDecision.AuthorizedAction != _actionProvider() ||
            snapshot.WarningState.Kind != WarningStateKind.Active)
        {
            return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.FinalCheckFailed);
        }

        return _freshFinalDecision;
    }

    private NightDecision EvaluateFreshAuthorization(
        SleepShutdownSettings settings,
        IdleSnapshot idle,
        ContextSnapshot context,
        DateTimeOffset now)
    {
        if (!settings.Enabled)
        {
            return NightDecision.Blocked(NightDecisionKind.Disabled, DecisionReasonCode.RoutineDisabled);
        }

        if (!MonitoringSchedule.IsInsideEvaluationWindow(settings, now))
        {
            return NightDecision.Blocked(NightDecisionKind.OutsideSchedule, DecisionReasonCode.OutsideNightWindow);
        }

        if (idle.InputDetected || idle.IdleDuration <= settings.IdleThreshold)
        {
            return NightDecision.Blocked(NightDecisionKind.WaitingForIdle, DecisionReasonCode.IdleThresholdNotMet);
        }

        if (ContextBlockingPolicy.BlocksShutdown(settings, idle, context))
        {
            return NightDecision.Blocked(NightDecisionKind.Protected, DecisionReasonCode.CriticalProtectionActive);
        }

        return NightDecision.Ready(
            _actionProvider(),
            DecisionReasonCode.Ready,
            warningDuration: null,
            kind: NightDecisionKind.AuthorizedToExecute);
    }
}
