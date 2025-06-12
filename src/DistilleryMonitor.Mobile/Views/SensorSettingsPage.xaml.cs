namespace DistilleryMonitor.Mobile.Views;
using DistilleryMonitor.Mobile.ViewModels;

public partial class SensorSettingsPage : ContentPage
{
    public SensorSettingsPage(SensorSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}