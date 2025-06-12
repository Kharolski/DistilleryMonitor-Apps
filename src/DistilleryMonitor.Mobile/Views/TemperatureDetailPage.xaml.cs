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

        // Uppdatera grafen först (för att få senaste inställningarna)
        await RefreshGraphSettingsAsync();

        // kör ViewModels OnAppearing
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
            // Hitta TemperatureGraphView i UI-trädet
            if (Content is ScrollView scrollView &&
                scrollView.Content is Grid grid)
            {
                // Andra Frame:et innehåller grafen (enligt din XAML)
                var graphFrame = grid.Children.OfType<Frame>().Skip(1).FirstOrDefault();
                if (graphFrame?.Content is TemperatureGraphView graphView)
                {
                    await graphView.RefreshSettingsAsync();
                    System.Diagnostics.Debug.WriteLine("Graf-inställningar uppdaterade!");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fel vid uppdatering av graf-inställningar: {ex.Message}");
        }
    }
}
