using Hushward.Application.Abstractions;
using Hushward.Application.Results;

namespace Hushward.Application.Configuration;

public sealed class ConfigurationCoordinator
{
    private readonly IConfigurationStore _store;

    public ConfigurationCoordinator(IConfigurationStore store)
    {
        _store = store;
    }

    public Task<OperationResult<string?>> ReadRawAsync(CancellationToken cancellationToken) =>
        _store.ReadJsonAsync(cancellationToken);
}
