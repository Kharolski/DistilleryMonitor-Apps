namespace DistilleryMonitor.Mobile.Views;
using DistilleryMonitor.Mobile.ViewModels;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

}
