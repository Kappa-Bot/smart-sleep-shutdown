namespace SmartSleepShutdown.Core.Abstractions;

public interface IShutdownExecutor
{
    Task ShutdownNowAsync(CancellationToken cancellationToken);
}
