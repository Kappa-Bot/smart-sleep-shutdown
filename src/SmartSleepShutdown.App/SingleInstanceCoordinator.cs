using System.Security.Principal;

namespace SmartSleepShutdown.App;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Semaphore _instanceSemaphore;
    private readonly EventWaitHandle _activationEvent;
    private readonly EventWaitHandle _exitEvent;
    private readonly EventWaitHandle _scheduledCheckEvent;
    private readonly CancellationTokenSource _listenerCancellation = new();
    private bool _disposed;

    private SingleInstanceCoordinator(
        Semaphore instanceSemaphore,
        EventWaitHandle activationEvent,
        EventWaitHandle exitEvent,
        EventWaitHandle scheduledCheckEvent,
        bool isPrimaryInstance)
    {
        _instanceSemaphore = instanceSemaphore;
        _activationEvent = activationEvent;
        _exitEvent = exitEvent;
        _scheduledCheckEvent = scheduledCheckEvent;
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public static SingleInstanceCoordinator CreateDefault()
    {
        var userId = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var suffix = SanitizeKernelObjectName(userId);
        return Create(
            $@"Local\SmartSleepShutdown-{suffix}-Instance",
            $@"Local\SmartSleepShutdown-{suffix}-Activate",
            $@"Local\SmartSleepShutdown-{suffix}-Exit",
            $@"Local\SmartSleepShutdown-{suffix}-Check");
    }

    public static SingleInstanceCoordinator Create(string instanceName, string activationEventName, string exitEventName)
    {
        return Create(instanceName, activationEventName, exitEventName, $"{instanceName}-Check");
    }

    public static SingleInstanceCoordinator Create(
        string instanceName,
        string activationEventName,
        string exitEventName,
        string scheduledCheckEventName)
    {
        var instanceSemaphore = new Semaphore(1, 1, instanceName);
        var isPrimaryInstance = instanceSemaphore.WaitOne(0);
        var activationEvent = isPrimaryInstance
            ? new EventWaitHandle(false, EventResetMode.AutoReset, activationEventName)
            : OpenOrCreateEvent(activationEventName);
        var exitEvent = isPrimaryInstance
            ? new EventWaitHandle(false, EventResetMode.AutoReset, exitEventName)
            : OpenOrCreateEvent(exitEventName);
        var scheduledCheckEvent = isPrimaryInstance
            ? new EventWaitHandle(false, EventResetMode.AutoReset, scheduledCheckEventName)
            : OpenOrCreateEvent(scheduledCheckEventName);

        return new SingleInstanceCoordinator(instanceSemaphore, activationEvent, exitEvent, scheduledCheckEvent, isPrimaryInstance);
    }

    public void StartActivationListener(Action activate)
    {
        StartListener(_activationEvent, activate);
    }

    public void StartExitListener(Action exit)
    {
        StartListener(_exitEvent, exit);
    }

    public void StartScheduledCheckListener(Action scheduledCheck)
    {
        StartListener(_scheduledCheckEvent, scheduledCheck);
    }

    public void SignalPrimaryInstance()
    {
        if (!IsPrimaryInstance)
        {
            _activationEvent.Set();
        }
    }

    public void SignalPrimaryExit()
    {
        if (!IsPrimaryInstance)
        {
            _exitEvent.Set();
        }
    }

    public void SignalPrimaryScheduledCheck()
    {
        if (!IsPrimaryInstance)
        {
            _scheduledCheckEvent.Set();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _listenerCancellation.Cancel();
        _listenerCancellation.Dispose();
        _activationEvent.Dispose();
        _exitEvent.Dispose();
        _scheduledCheckEvent.Dispose();

        if (IsPrimaryInstance)
        {
            _instanceSemaphore.Release();
        }

        _instanceSemaphore.Dispose();
    }

    private void StartListener(EventWaitHandle eventWaitHandle, Action action)
    {
        if (!IsPrimaryInstance)
        {
            return;
        }

        var cancellationToken = _listenerCancellation.Token;
        _ = Task.Run(() =>
        {
            var handles = new WaitHandle[] { eventWaitHandle, cancellationToken.WaitHandle };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var signaled = WaitHandle.WaitAny(handles);
                    if (signaled == 0)
                    {
                        action();
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }, CancellationToken.None);
    }

    private static EventWaitHandle OpenOrCreateEvent(string eventName)
    {
        try
        {
            return EventWaitHandle.OpenExisting(eventName);
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
        }
    }

    private static string SanitizeKernelObjectName(string value)
    {
        return string.Concat(value.Select(character =>
            char.IsAsciiLetterOrDigit(character) ? character : '-'));
    }
}
