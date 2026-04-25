using System.Runtime.InteropServices;
using SmartSleepShutdown.Core.Models;

namespace SmartSleepShutdown.Infrastructure.System;

public sealed class AudioPlayingContextProbe : IContextProbe
{
    private const float PeakThreshold = 0.01f;
    private static readonly Guid AudioMeterInformationId = new("C02216F6-8C67-4B5B-9D00-D008E73E0064");

    public ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IAudioEndpointEnumerator? enumerator = null;
        IAudioDevice? device = null;
        object? meterObject = null;

        try
        {
            enumerator = (IAudioEndpointEnumerator)(object)new MmDeviceEnumerator();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device);
            var meterId = AudioMeterInformationId;
            device.Activate(ref meterId, ClsCtx.InprocServer, IntPtr.Zero, out meterObject);
            var meter = (IAudioMeterInformation)meterObject;
            meter.GetPeakValue(out var peak);

            var context = peak > PeakThreshold
                ? new BlockingContext(BlockingContextType.AudioPlaying, "Audio is playing")
                : null;

            return ValueTask.FromResult<BlockingContext?>(context);
        }
        catch (COMException ex) when (IsExpectedNoAudioDeviceFailure(ex))
        {
            return ValueTask.FromResult<BlockingContext?>(null);
        }
        finally
        {
            ReleaseComObject(meterObject);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
        }
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.FinalReleaseComObject(comObject);
        }
    }

    public static bool IsExpectedNoAudioDeviceFailure(COMException exception)
    {
        return exception.HResult is unchecked((int)0x80070490) // HRESULT_FROM_WIN32(ERROR_NOT_FOUND)
            or unchecked((int)0x88890004); // AUDCLNT_E_DEVICE_INVALIDATED
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    private enum ERole
    {
        Console,
        Multimedia,
        Communications
    }

    [Flags]
    private enum ClsCtx : uint
    {
        InprocServer = 0x1
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MmDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr devices);

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IAudioDevice endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioDevice
    {
        void Activate(ref Guid iid, ClsCtx dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
    }

    [ComImport]
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        void GetPeakValue(out float peak);

        void GetMeteringChannelCount(out uint channelCount);

        void GetChannelsPeakValues(uint channelCount, [Out] float[] peakValues);

        void QueryHardwareSupport(out uint hardwareSupportMask);
    }
}
