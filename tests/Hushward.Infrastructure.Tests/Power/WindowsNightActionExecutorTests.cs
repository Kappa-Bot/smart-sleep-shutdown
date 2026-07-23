using Hushward.Core.Actions;
using Hushward.Infrastructure.Power;
using Hushward.Infrastructure.Sessions;

namespace Hushward.Infrastructure.Tests.Power;

public sealed class WindowsNightActionExecutorTests
{
    [Fact]
    public async Task Shutdown_uses_exact_non_forced_arguments()
    {
        var process = new RecordingProcessLauncher();
        var executor = new WindowsNightActionExecutor(
            process,
            new RecordingPowerApi(),
            new RecordingSessionApi());

        var result = await executor.ExecuteAsync(NightAction.ShutDown, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("shutdown.exe", process.Request!.FileName);
        Assert.Equal(["/s", "/t", "0"], process.Request.ArgumentList);
        Assert.DoesNotContain("/f", process.Request.ArgumentList);
    }

    [Fact]
    public async Task Unsupported_hibernate_returns_typed_failure_without_fallback()
    {
        var power = new RecordingPowerApi(hibernateSupported: false);
        var executor = new WindowsNightActionExecutor(
            new RecordingProcessLauncher(),
            power,
            new RecordingSessionApi());

        var result = await executor.ExecuteAsync(NightAction.Hibernate, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("power.hibernate.unsupported", result.Error!.Code);
        Assert.Empty(power.SuspendRequests);
    }

    [Fact]
    public async Task Sleep_uses_non_forced_suspend_api_after_capability_check()
    {
        var power = new RecordingPowerApi(sleepSupported: true);
        var executor = new WindowsNightActionExecutor(
            new RecordingProcessLauncher(),
            power,
            new RecordingSessionApi());

        var result = await executor.ExecuteAsync(NightAction.Sleep, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([false], power.SuspendRequests);
    }

    [Fact]
    public async Task Warn_only_does_not_invoke_os_action()
    {
        var process = new RecordingProcessLauncher();
        var power = new RecordingPowerApi();
        var session = new RecordingSessionApi();
        var executor = new WindowsNightActionExecutor(process, power, session);

        var result = await executor.ExecuteAsync(NightAction.WarnOnly, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(process.Request);
        Assert.Empty(power.SuspendRequests);
        Assert.False(session.LockInvoked);
    }

    [Fact]
    public async Task Lock_invokes_session_api_only()
    {
        var process = new RecordingProcessLauncher();
        var power = new RecordingPowerApi();
        var session = new RecordingSessionApi();
        var executor = new WindowsNightActionExecutor(process, power, session);

        var result = await executor.ExecuteAsync(NightAction.Lock, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(session.LockInvoked);
        Assert.Null(process.Request);
        Assert.Empty(power.SuspendRequests);
    }

    private sealed class RecordingProcessLauncher : IProcessLauncher
    {
        public ProcessLaunchRequest? Request { get; private set; }

        public Task<int> LaunchAsync(ProcessLaunchRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(0);
        }
    }

    private sealed class RecordingPowerApi : IWindowsPowerApi
    {
        private readonly bool _sleepSupported;
        private readonly bool _hibernateSupported;

        public RecordingPowerApi(bool sleepSupported = true, bool hibernateSupported = true)
        {
            _sleepSupported = sleepSupported;
            _hibernateSupported = hibernateSupported;
        }

        public List<bool> SuspendRequests { get; } = [];

        public WindowsPowerLineStatus ReadLineStatus() => new(false, 80, true);

        public WindowsPowerCapabilities ReadCapabilities() => new(_sleepSupported, _hibernateSupported);

        public bool SetSuspendState(bool hibernate)
        {
            SuspendRequests.Add(hibernate);
            return true;
        }
    }

    private sealed class RecordingSessionApi : IWindowsSessionApi
    {
        public bool LockInvoked { get; private set; }

        public WindowsSessionSnapshot ReadSession() => new(false, false);

        public bool LockWorkStation()
        {
            LockInvoked = true;
            return true;
        }
    }
}
