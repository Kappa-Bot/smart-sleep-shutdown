using Hushward.Application.Abstractions;
using Hushward.Core.Actions;
using Hushward.Core.Abstractions;

namespace Hushward.Infrastructure.Power;

public sealed class WindowsShutdownExecutor : IShutdownExecutor
{
    private readonly INightActionExecutor _actionExecutor;

    public WindowsShutdownExecutor()
        : this(new WindowsNightActionExecutor())
    {
    }

    public WindowsShutdownExecutor(INightActionExecutor actionExecutor)
    {
        _actionExecutor = actionExecutor;
    }

    public async Task ShutdownNowAsync(CancellationToken cancellationToken)
    {
        var result = await _actionExecutor.ExecuteAsync(NightAction.ShutDown, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.TechnicalDetail ?? result.Error.MessageKey}");
        }
    }
}
