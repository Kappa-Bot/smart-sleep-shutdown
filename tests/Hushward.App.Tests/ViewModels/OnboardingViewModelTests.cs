using Hushward.App.ViewModels.Onboarding;
using Hushward.Core.Routines;

namespace Hushward.App.Tests.ViewModels;

public sealed class OnboardingViewModelTests
{
    [Fact]
    public async Task RoutineRemainsDisabledUntilSummaryIsConfirmed()
    {
        NightRoutine? saved = null;
        var viewModel = new OnboardingViewModel(routine =>
        {
            saved = routine;
            return Task.CompletedTask;
        });

        await viewModel.NextAsync();
        await viewModel.NextAsync();
        await viewModel.NextAsync();

        Assert.NotNull(saved);
        Assert.False(saved.Enabled);
        Assert.False(viewModel.IsComplete);

        await viewModel.ConfirmAndEnableAsync();

        Assert.True(saved.Enabled);
        Assert.True(viewModel.IsComplete);
        Assert.Contains("Siempre avisará", viewModel.Summary);
    }
}
