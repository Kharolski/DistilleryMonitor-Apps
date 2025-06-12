using DistilleryMonitor.Mobile.Components;
using DistilleryMonitor.Mobile.ViewModels;

namespace DistilleryMonitor.Mobile.Views;

public partial class TemperatureDetailPage : ContentPage
{
    private readonly TemperatureDetailViewModel _viewModel;

    public TemperatureDetailPage(TemperatureDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Uppdatera grafen f�rst (f�r att f� senaste inst�llningarna)
        await RefreshGraphSettingsAsync();

        // k�r ViewModels OnAppearing
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();

    }

    private async Task RefreshGraphSettingsAsync()
    {
        try
        {
            // Hitta TemperatureGraphView i UI-tr�det
            if (Content is ScrollView scrollView &&
                scrollView.Content is Grid grid)
            {
                // Andra Frame:et inneh�ller grafen (enligt din XAML)
                var graphFrame = grid.Children.OfType<Frame>().Skip(1).FirstOrDefault();
                if (graphFrame?.Content is TemperatureGraphView graphView)
                {
                    await graphView.RefreshSettingsAsync();
                    System.Diagnostics.Debug.WriteLine("Graf-inst�llningar uppdaterade!");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fel vid uppdatering av graf-inst�llningar: {ex.Message}");
        }
    }
}
