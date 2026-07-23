namespace Hushward.App;

public enum TrayVisualState
{
    Off,
    Ready,
    Active = Ready,
    Waiting,
    Protected,
    Warning,
    Degraded,
    SuspendedToday
}
