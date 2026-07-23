using System.Diagnostics;
using Hushward.Infrastructure.Interop;

namespace Hushward.Infrastructure.Power;

internal sealed record ProcessLaunchRequest(
    string FileName,
    IReadOnlyList<string> ArgumentList,
    bool UseShellExecute,
    bool CreateNoWindow);

internal interface IProcessLauncher
{
    Task<int> LaunchAsync(ProcessLaunchRequest request, CancellationToken cancellationToken);
}

internal sealed class ProcessLauncher : IProcessLauncher
{
    public async Task<int> LaunchAsync(ProcessLaunchRequest request, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName = request.FileName,
                UseShellExecute = request.UseShellExecute,
                CreateNoWindow = request.CreateNoWindow
            }
        };

        foreach (var argument in request.ArgumentList)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Process start returned false.");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}

internal sealed record WindowsPowerLineStatus(
    bool? IsOnBattery,
    int? BatteryPercent,
    bool? IsCharging);

internal sealed record WindowsPowerCapabilities(
    bool SleepSupported,
    bool HibernateSupported);

internal interface IWindowsPowerApi
{
    WindowsPowerLineStatus ReadLineStatus();

    WindowsPowerCapabilities ReadCapabilities();

    bool SetSuspendState(bool hibernate);
}

internal sealed class WindowsPowerApi : IWindowsPowerApi
{
    public WindowsPowerLineStatus ReadLineStatus()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            throw new InvalidOperationException("GetSystemPowerStatus failed.");
        }

        bool? isOnBattery = status.AcLineStatus switch
        {
            0 => true,
            1 => false,
            _ => null
        };

        int? batteryPercent = status.BatteryLifePercent == 255 ? null : status.BatteryLifePercent;
        bool? isCharging = status.BatteryFlag == 255 ? null : (status.BatteryFlag & 8) == 8;
        return new WindowsPowerLineStatus(isOnBattery, batteryPercent, isCharging);
    }

    public WindowsPowerCapabilities ReadCapabilities()
    {
        if (!NativeMethods.GetPwrCapabilities(out var capabilities))
        {
            throw new InvalidOperationException("GetPwrCapabilities failed.");
        }

        return new WindowsPowerCapabilities(
            SleepSupported: capabilities.SystemS1 || capabilities.SystemS2 || capabilities.SystemS3,
            HibernateSupported: capabilities.SystemS4 && capabilities.HiberFilePresent);
    }

    public bool SetSuspendState(bool hibernate) =>
        NativeMethods.SetSuspendState(hibernate, forceCritical: false, disableWakeEvent: false);
}
