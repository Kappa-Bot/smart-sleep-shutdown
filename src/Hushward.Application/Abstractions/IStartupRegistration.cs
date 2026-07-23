using Hushward.Application.Results;

namespace Hushward.Application.Abstractions;

public sealed record StartupRegistrationState(bool Enabled, bool Healthy, string? ErrorCode);

public interface IStartupRegistration
{
    Task<OperationResult<StartupRegistrationState>> ReadAsync(CancellationToken cancellationToken);

    Task<OperationResult<StartupRegistrationState>> SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken);
}
