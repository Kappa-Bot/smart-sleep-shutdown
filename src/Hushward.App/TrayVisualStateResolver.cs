using Hushward.App.ViewModels;
using Hushward.App.Runtime;

namespace Hushward.App;

public static class TrayVisualStateResolver
{
    public static TrayVisualState Resolve(ShellViewModel viewModel)
    {
        if (viewModel.IsTemporarilyDisabled)
        {
            return TrayVisualState.SuspendedToday;
        }

        return viewModel.IsEnabled ? TrayVisualState.Active : TrayVisualState.Off;
    }

    public static TrayVisualState Resolve(NightMonitorController viewModel)
    {
        if (viewModel.IsTemporarilyDisabled)
        {
            return TrayVisualState.SuspendedToday;
        }

        return viewModel.IsEnabled ? TrayVisualState.Active : TrayVisualState.Off;
    }
}
