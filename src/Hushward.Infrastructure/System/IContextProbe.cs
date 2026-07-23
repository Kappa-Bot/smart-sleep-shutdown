using Hushward.Core.Models;

namespace Hushward.Infrastructure.System;

public interface IContextProbe
{
    ValueTask<BlockingContext?> DetectAsync(CancellationToken cancellationToken);
}

