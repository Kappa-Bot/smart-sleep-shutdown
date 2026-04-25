namespace SmartSleepShutdown.Core.Models;

public enum BlockingContextType
{
    FullScreenApp,
    AudioPlaying,
    HighCpu,
    KnownProcess,
    DetectorFailure
}
