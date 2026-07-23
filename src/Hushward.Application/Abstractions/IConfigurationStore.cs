using Hushward.Application.Results;

namespace Hushward.Application.Abstractions;

public interface IConfigurationStore
{
    Task<OperationResult<string?>> ReadJsonAsync(CancellationToken cancellationToken);

    Task<OperationResult<Unit>> WriteJsonAsync(string json, CancellationToken cancellationToken);
}
