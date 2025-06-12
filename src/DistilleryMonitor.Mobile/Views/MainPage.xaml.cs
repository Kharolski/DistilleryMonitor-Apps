using DistilleryMonitor.Mobile.ViewModels;

namespace DistilleryMonitor.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        // Använd DisplayActionSheet för dropdown-känsla
        var action = await DisplayActionSheet(
            title: null,           // Ingen titel för cleaner look
            cancel: "Stäng",       // Stäng-knapp
            destruction: null,     // Ingen destruktiv action
            "🏠 Hem",             // Alternativ
            "⚙️ Inställningar",
            "ℹ️ Om Appen"
        );

        switch (action)
        {
            case "⚙️ Inställningar":
                await Shell.Current.GoToAsync("///settings");
                break;
            case "ℹ️ Om Appen":
                await Shell.Current.GoToAsync("///about");
                break;
            case "🏠 Hem":
                await Shell.Current.GoToAsync("///home"); 
                break;
        }
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Starta automatisk uppdatering när sidan visas
        if (BindingContext is MainPageViewModel viewModel)
        {
            await viewModel.StartAutoUpdateAsync();
        }
    }
}
