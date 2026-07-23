namespace Hushward.App.Accessibility;

public sealed class LiveRegionAnnouncer
{
    private static readonly HashSet<int> Milestones = [60, 30, 10, 5, 4, 3, 2, 1];
    private readonly Action<string> _announce;
    private int? _lastAnnounced;

    public LiveRegionAnnouncer(Action<string> announce)
    {
        _announce = announce;
    }

    public void Update(int remainingSeconds, Func<int, string> format)
    {
        if (!Milestones.Contains(remainingSeconds) || _lastAnnounced == remainingSeconds)
        {
            return;
        }

        _lastAnnounced = remainingSeconds;
        _announce(format(remainingSeconds));
    }

    public void Reset() => _lastAnnounced = null;
}
