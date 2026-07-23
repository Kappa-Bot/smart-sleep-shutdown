namespace Hushward.App.Runtime;

public interface IWarningSessionController
{
    Task StartAsync(TimeSpan duration, CancellationToken cancellationToken);

    Task InvalidateForInputAsync();

    Task InvalidateForProtectionAsync();
}
