namespace Hushward.Application.Runtime;

public sealed class RuntimeSnapshotPublisher : IObservable<NightRuntimeSnapshot>
{
    private readonly object _gate = new();
    private readonly List<IObserver<NightRuntimeSnapshot>> _observers = [];
    private NightRuntimeSnapshot _latest;

    public RuntimeSnapshotPublisher(NightRuntimeSnapshot initialSnapshot)
    {
        _latest = initialSnapshot;
    }

    public NightRuntimeSnapshot Latest
    {
        get
        {
            lock (_gate)
            {
                return _latest;
            }
        }
    }

    public IDisposable Subscribe(IObserver<NightRuntimeSnapshot> observer)
    {
        lock (_gate)
        {
            _observers.Add(observer);
            observer.OnNext(_latest);
        }

        return new Subscription(this, observer);
    }

    public void Publish(NightRuntimeSnapshot snapshot)
    {
        IObserver<NightRuntimeSnapshot>[] observers;
        lock (_gate)
        {
            if (!snapshot.IsNewerThan(_latest))
            {
                return;
            }

            _latest = snapshot;
            observers = _observers.ToArray();
        }

        foreach (var observer in observers)
        {
            observer.OnNext(snapshot);
        }
    }

    private void Unsubscribe(IObserver<NightRuntimeSnapshot> observer)
    {
        lock (_gate)
        {
            _observers.Remove(observer);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly RuntimeSnapshotPublisher _publisher;
        private readonly IObserver<NightRuntimeSnapshot> _observer;
        private bool _disposed;

        public Subscription(RuntimeSnapshotPublisher publisher, IObserver<NightRuntimeSnapshot> observer)
        {
            _publisher = publisher;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _publisher.Unsubscribe(_observer);
        }
    }
}
