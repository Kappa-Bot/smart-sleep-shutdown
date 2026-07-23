using System.ComponentModel;
using System.Windows;
using Hushward.App.ViewModels.Onboarding;

namespace Hushward.App.Views.Onboarding;

public partial class OnboardingWindow : Window
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingWindow(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnPropertyChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnPropertyChanged;
        base.OnClosed(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingViewModel.IsComplete) && _viewModel.IsComplete)
        {
            DialogResult = true;
            Close();
        }
    }
}
