using Hushward.Core.Models;

namespace Hushward.Core.Abstractions;

public interface IContextDetector
{
    ValueTask<ContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken);
}

