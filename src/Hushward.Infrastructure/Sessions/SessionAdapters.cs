using Hushward.Infrastructure.Interop;

namespace Hushward.Infrastructure.Sessions;

internal sealed record WindowsSessionSnapshot(
    bool IsLocked,
    bool IsRemote);

internal interface IWindowsSessionApi
{
    WindowsSessionSnapshot ReadSession();

    bool LockWorkStation();
}

internal sealed class WindowsSessionApi : IWindowsSessionApi
{
    public WindowsSessionSnapshot ReadSession() =>
        new(
            NativeMethods.IsWorkstationLocked(),
            NativeMethods.GetSystemMetrics(NativeMethods.SmRemoteSession) != 0);

    public bool LockWorkStation() => NativeMethods.LockWorkStation();
}
