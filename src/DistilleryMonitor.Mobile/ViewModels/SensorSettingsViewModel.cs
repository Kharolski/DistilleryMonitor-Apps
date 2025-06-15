using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using DistilleryMonitor.Core.Services; 

namespace DistilleryMonitor.Mobile.ViewModels;

[QueryProperty(nameof(SensorId), "sensorId")]
[QueryProperty(nameof(SensorName), "sensorName")]
public partial class SensorSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService; 

    [ObservableProperty] private int sensorId;
    [ObservableProperty] private string sensorName = string.Empty;

    // Bara 3 temperaturinställningar
    [ObservableProperty] private string optimalMin = "70";     // Optimal börjar vid
    [ObservableProperty] private string warningTemp = "85";    // Varning börjar vid
    [ObservableProperty] private string criticalTemp = "90";   // Kritisk börjar vid
    [ObservableProperty] private bool isLoading = false;

    // Ta emot ISettingsService via DI
    public SensorSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Debug.WriteLine("SensorSettingsViewModel created");
    }

    partial void OnSensorIdChanged(int value)
    {
        Debug.WriteLine($"SensorSettingsViewModel: SensorId changed to {value}");
        _ = LoadSensorSettingsAsync();
    }

    partial void OnSensorNameChanged(string value)
    {
        Debug.WriteLine($"SensorSettingsViewModel: SensorName changed to {value}");
        _ = LoadSensorSettingsAsync(); 
    }

    /// <summary>
    /// Ladda sparade inställningar från ISettingsService
    /// </summary>
    private async Task LoadSensorSettingsAsync()
    {
        if (SensorId < 0 || string.IsNullOrEmpty(SensorName))
            return;

        try
        {
            // Läs från ISettingsService istället för Preferences
            switch (SensorName)
            {
                case "Kolv":
                    OptimalMin = (await _settingsService.GetKolvOptimalMinAsync()).ToString();
                    WarningTemp = (await _settingsService.GetKolvWarningTempAsync()).ToString();
                    CriticalTemp = (await _settingsService.GetKolvCriticalTempAsync()).ToString();
                    break;

                case "Destillat":
                    OptimalMin = (await _settingsService.GetDestillatOptimalMinAsync()).ToString();
                    WarningTemp = (await _settingsService.GetDestillatWarningTempAsync()).ToString();
                    CriticalTemp = (await _settingsService.GetDestillatCriticalTempAsync()).ToString();
                    break;

                case "Kylare":
                    OptimalMin = (await _settingsService.GetKylareOptimalMinAsync()).ToString();
                    WarningTemp = (await _settingsService.GetKylareWarningTempAsync()).ToString();
                    CriticalTemp = (await _settingsService.GetKylareCriticalTempAsync()).ToString();
                    break;

                default:
                    SetDefaultValues();
                    break;
            }

            Debug.WriteLine($"Loaded settings for {SensorName}: Optimal från {OptimalMin}°C, Varning {WarningTemp}°C");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading sensor settings: {ex.Message}");
            SetDefaultValues();
        }
    }

    /// <summary>
    /// Spara inställningar till ISettingsService
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;

            if (!ValidateTemperatureValues())
            {
                await ShowErrorAsync("Ogiltiga temperaturvärden. Kontrollera att: Optimal < Varning < Kritisk");
                return;
            }

            var optimal = double.Parse(OptimalMin);
            var warning = double.Parse(WarningTemp);
            var critical = double.Parse(CriticalTemp);

            // Spara till ISettingsService istället för Preferences
            switch (SensorName)
            {
                case "Kolv":
                    await _settingsService.SetKolvOptimalMinAsync(optimal);
                    await _settingsService.SetKolvWarningTempAsync(warning);
                    await _settingsService.SetKolvCriticalTempAsync(critical);
                    break;

                case "Destillat":
                    await _settingsService.SetDestillatOptimalMinAsync(optimal);
                    await _settingsService.SetDestillatWarningTempAsync(warning);
                    await _settingsService.SetDestillatCriticalTempAsync(critical);
                    break;

                case "Kylare":
                    await _settingsService.SetKylareOptimalMinAsync(optimal);
                    await _settingsService.SetKylareWarningTempAsync(warning);
                    await _settingsService.SetKylareCriticalTempAsync(critical);
                    break;
            }

            Debug.WriteLine($"Saved settings for {SensorName}: {OptimalMin}° → {WarningTemp}° → {CriticalTemp}°");

            await Application.Current.MainPage.DisplayAlert(
                "Sparat! ✅",
                $"Inställningar för {SensorName} har sparats.\n" +
                $"🟢 Optimal: från {OptimalMin}°C\n" +
                $"🟡 Varning: från {WarningTemp}°C\n" +
                $"🔴 Kritisk: från {CriticalTemp}°C",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving settings: {ex.Message}");
            await ShowErrorAsync("Kunde inte spara inställningar. Försök igen.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        var result = await Application.Current.MainPage.DisplayAlert(
            "Återställ inställningar",
            $"Vill du återställa {SensorName} till standardvärden?",
            "Ja", "Avbryt");

        if (result)
        {
            SetDefaultValues();
            await SaveAsync(); // Spara standardvärdena

            await Application.Current.MainPage.DisplayAlert(
                "Återställt! 🔄",
                "Inställningar återställda till standardvärden.",
                "OK");
        }
    }

    private void SetDefaultValues()
    {
        OptimalMin = GetDefaultOptimalMin();
        WarningTemp = GetDefaultWarningTemp();
        CriticalTemp = GetDefaultCriticalTemp();
    }

    // Standardvärden för nya logiken
    private string GetDefaultOptimalMin()
    {
        return SensorName switch
        {
            "Kolv" => "70",      // Optimal börjar vid 70°C
            "Destillat" => "75", // Optimal börjar vid 75°C
            "Kylare" => "20",    // Optimal börjar vid 20°C
            _ => "50"
        };
    }

    private string GetDefaultWarningTemp()
    {
        return SensorName switch
        {
            "Kolv" => "80",      // 80 istället för 85 (matchar dina defaults)
            "Destillat" => "85", // 85 istället för 90
            "Kylare" => "30",    // 30 istället för 35
            _ => "80"
        };
    }

    private string GetDefaultCriticalTemp()
    {
        return SensorName switch
        {
            "Kolv" => "90",      // Kritisk börjar vid 90°C
            "Destillat" => "95", // Kritisk börjar vid 95°C
            "Kylare" => "40",    // Kritisk börjar vid 40°C
            _ => "90"
        };
    }

    /// <summary>
    /// Validering för nya logiken
    /// </summary>
    private bool ValidateTemperatureValues()
    {
        try
        {
            var optimal = double.Parse(OptimalMin);
            var warning = double.Parse(WarningTemp);
            var critical = double.Parse(CriticalTemp);

            // Ny logik: optimal < warning < critical
            return optimal < warning && warning < critical;
        }
        catch
        {
            return false;
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        await Application.Current.MainPage.DisplayAlert("Fel ❌", message, "OK");
    }
}
