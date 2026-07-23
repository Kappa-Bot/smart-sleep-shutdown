using Hushward.Application.Results;
using Hushward.Core.Protections;

namespace Hushward.Application.Abstractions;

public interface IProtectionDetector
{
    string Id { get; }

    Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken);
}
