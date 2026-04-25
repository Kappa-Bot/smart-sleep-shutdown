using SmartSleepShutdown.App.ViewModels;

namespace SmartSleepShutdown.App;

public static class TrayVisualStateResolver
{
    public static TrayVisualState Resolve(MainWindowViewModel viewModel)
    {
        if (viewModel.IsTemporarilyDisabled)
        {
            return TrayVisualState.SuspendedToday;
        }

        return viewModel.IsEnabled ? TrayVisualState.Active : TrayVisualState.Off;
    }
}
