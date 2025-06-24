using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services; 
using System.Diagnostics;

namespace DistilleryMonitor.Mobile.ViewModels;

[QueryProperty(nameof(SensorId), "sensorId")]
[QueryProperty(nameof(SensorName), "sensorName")]
public partial class SensorSettingsViewModel : ObservableObject
{
    #region Private Fields
    private readonly ISettingsService _settingsService;
    private readonly ApiService _apiService;
    #endregion 

    #region Constructor
    /// <summary>
    /// Konstruktor - tar emot services via DI
    /// </summary>
    public SensorSettingsViewModel(ISettingsService settingsService, ApiService apiService)
    {
        _settingsService = settingsService;
        _apiService = apiService;
        Debug.WriteLine("SensorSettingsViewModel created");
    }
    #endregion

    #region Observable Properties
    [ObservableProperty] private int sensorId;
    [ObservableProperty] private string sensorName = string.Empty;

    // Temperaturinställningar (som strings för UI-binding)
    [ObservableProperty] private string optimalMin = "70";     // Optimal börjar vid
    [ObservableProperty] private string warningTemp = "85";    // Varning börjar vid
    [ObservableProperty] private string criticalTemp = "90";   // Kritisk börjar vid

    // UI State
    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool useMockData = false;
    [ObservableProperty] private string connectionStatus = "";
    [ObservableProperty] private bool isEsp32Available = false;
    #endregion

    #region Navigation Properties Changed
    /// <summary>
    /// Körs när SensorId ändras från navigation
    /// </summary>
    partial void OnSensorIdChanged(int value)
    {
        Debug.WriteLine($"SensorSettingsViewModel: SensorId changed to {value}");
        _ = LoadSensorSettingsAsync();
    }

    /// <summary>
    /// Körs när SensorName ändras från navigation
    /// </summary>
    partial void OnSensorNameChanged(string value)
    {
        Debug.WriteLine($"SensorSettingsViewModel: SensorName changed to {value}");
        _ = LoadSensorSettingsAsync();
    }
    #endregion

    #region Settings Loading (ESP32 + Local)
    /// <summary>
    /// Laddar sparade inställningar - FÖRST från ESP32, sedan lokala settings
    /// </summary>
    private async Task LoadSensorSettingsAsync()
    {
        if (SensorId < 0 || string.IsNullOrEmpty(SensorName))
            return;

        try
        {
            IsLoading = true;

            // Kontrollera om vi använder mock data
            UseMockData = await _settingsService.GetUseMockDataAsync();

            // Försök först ESP32 (om inte mock data)
            bool loadedFromEsp32 = false;
            if (!UseMockData)
            {
                loadedFromEsp32 = await LoadFromEsp32Async();
            }

            // Fallback till lokala settings om ESP32 misslyckades
            if (!loadedFromEsp32)
            {
                await LoadFromLocalSettingsAsync();
            }

            UpdateConnectionStatus(loadedFromEsp32);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading sensor settings: {ex.Message}");
            SetDefaultValues();
            ConnectionStatus = "❌ Fel vid laddning";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Laddar inställningar från ESP32
    /// </summary>
    private async Task<bool> LoadFromEsp32Async()
    {
        try
        {
            var esp32Settings = await _apiService.GetSensorSettingsAsync(SensorName);
            if (esp32Settings != null)
            {
                OptimalMin = esp32Settings.OptimalMin.ToString("F1");
                WarningTemp = esp32Settings.WarningTemp.ToString("F1");
                CriticalTemp = esp32Settings.CriticalTemp.ToString("F1");

                IsEsp32Available = true;
                return true;
            }
            else
            {
                Debug.WriteLine($"❌ Kunde inte ladda från ESP32 för {SensorName}");
                IsEsp32Available = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fel vid ESP32-laddning: {ex.Message}");
            IsEsp32Available = false;
            return false;
        }
    }

    /// <summary>
    /// Laddar inställningar från lokala settings (URSPRUNGLIG LOGIK)
    /// </summary>
    private async Task LoadFromLocalSettingsAsync()
    {
        try
        {
            switch (SensorName)
            {
                case "Kolv":
                    OptimalMin = (await _settingsService.GetKolvOptimalMinAsync()).ToString("F1");
                    WarningTemp = (await _settingsService.GetKolvWarningTempAsync()).ToString("F1");
                    CriticalTemp = (await _settingsService.GetKolvCriticalTempAsync()).ToString("F1");
                    break;
                case "Destillat":
                    OptimalMin = (await _settingsService.GetDestillatOptimalMinAsync()).ToString("F1");
                    WarningTemp = (await _settingsService.GetDestillatWarningTempAsync()).ToString("F1");
                    CriticalTemp = (await _settingsService.GetDestillatCriticalTempAsync()).ToString("F1");
                    break;
                case "Kylare":
                    OptimalMin = (await _settingsService.GetKylareOptimalMinAsync()).ToString("F1");
                    WarningTemp = (await _settingsService.GetKylareWarningTempAsync()).ToString("F1");
                    CriticalTemp = (await _settingsService.GetKylareCriticalTempAsync()).ToString("F1");
                    break;
                default:
                    SetDefaultValues();
                    break;
            }

            IsEsp32Available = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fel vid laddning av lokala settings: {ex.Message}");
            SetDefaultValues();
        }
    }

    /// <summary>
    /// Uppdaterar connection status baserat på datakälla
    /// </summary>
    private void UpdateConnectionStatus(bool fromEsp32)
    {
        if (UseMockData)
        {
            ConnectionStatus = "🧪 Testdata - inställningar";
        }
        else if (fromEsp32)
        {
            ConnectionStatus = "✅ Sensor aktiv - inställningar";
        }
        else
        {
            ConnectionStatus = "📱 Lokala inställningar - ESP32 ej tillgänglig";
        }
    }
    #endregion

    #region Save Commands
    /// <summary>
    /// Spara inställningar - BÅDE till ESP32 OCH lokala settings
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

            bool esp32Success = false;
            bool localSuccess = false;

            // Spara till ESP32 först (om tillgängligt)
            if (!UseMockData)
            {
                esp32Success = await SaveToEsp32Async(optimal, warning, critical);
            }

            // Spara alltid till lokala settings som backup
            localSuccess = await SaveToLocalSettingsAsync(optimal, warning, critical);

            // Visa resultat
            await ShowSaveResultAsync(esp32Success, localSuccess);

            if (localSuccess) // Navigera tillbaka om åtminstone lokala settings sparades
            {
                await Shell.Current.GoToAsync("..");
            }
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

    /// <summary>
    /// Sparar inställningar till ESP32
    /// </summary>
    private async Task<bool> SaveToEsp32Async(double optimal, double warning, double critical)
    {
        try
        {
            Debug.WriteLine($"💾 Sparar till ESP32 för {SensorName}");

            var settings = new SensorSettings
            {
                SensorName = SensorName,
                OptimalMin = optimal,
                WarningTemp = warning,
                CriticalTemp = critical
            };

            bool success = await _apiService.SetSensorSettingsAsync(SensorName, settings);

            if (success)
            {
                Debug.WriteLine($"✅ ESP32-sparning lyckades för {SensorName}");
                IsEsp32Available = true;
                return true;
            }
            else
            {
                Debug.WriteLine($"❌ ESP32-sparning misslyckades för {SensorName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fel vid ESP32-sparning: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sparar inställningar till lokala settings (URSPRUNGLIG LOGIK)
    /// </summary>
    private async Task<bool> SaveToLocalSettingsAsync(double optimal, double warning, double critical)
    {
        try
        {
            Debug.WriteLine($"💾 Sparar till lokala settings för {SensorName}");

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
                default:
                    return false;
            }

            Debug.WriteLine($"✅ Lokala settings sparade för {SensorName}: {optimal}° → {warning}° → {critical}°");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fel vid sparning av lokala settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Visar resultat av sparning
    /// </summary>
    private async Task ShowSaveResultAsync(bool esp32Success, bool localSuccess)
    {
        string title;
        string message;

        if (esp32Success && localSuccess)
        {
            title = "Sparat! ✅";
            message = $"Inställningar för {SensorName} har sparats till BÅDE ESP32 och lokalt.\n" +
                     $"🟢 Optimal: från {OptimalMin}°C\n" +
                     $"🟡 Varning: från {WarningTemp}°C\n" +
                     $"🔴 Kritisk: från {CriticalTemp}°C";
            ConnectionStatus = "✅ ESP32 + Lokalt - Fullständigt synkroniserat";
        }
        else if (!esp32Success && localSuccess)
        {
            title = "Delvis sparat ⚠️";
            message = $"Inställningar sparade lokalt men kunde inte nå ESP32.\n" +
                     $"🟢 Optimal: från {OptimalMin}°C\n" +
                     $"🟡 Varning: från {WarningTemp}°C\n" +
                     $"🔴 Kritisk: från {CriticalTemp}°C\n\n" +
                     $"ESP32 kommer synkroniseras när anslutning återställs.";
            ConnectionStatus = "📱 Endast lokalt - ESP32 ej tillgänglig";
        }
        else if (esp32Success && !localSuccess)
        {
            title = "Delvis sparat ⚠️";
            message = $"Inställningar sparade till ESP32 men lokala settings misslyckades.\n" +
                     $"🟢 Optimal: från {OptimalMin}°C\n" +
                     $"🟡 Varning: från {WarningTemp}°C\n" +
                     $"🔴 Kritisk: från {CriticalTemp}°C";
            ConnectionStatus = "✅ ESP32 - Lokala settings misslyckades";
        }
        else
        {
            title = "Fel ❌";
            message = "Kunde inte spara inställningar varken till ESP32 eller lokalt. Försök igen.";
            ConnectionStatus = "❌ Sparning misslyckades";
            return; // Visa inte success-dialog
        }

        await Application.Current.MainPage.DisplayAlert(title, message, "OK");
    }
    #endregion

    #region Reset Command
    /// <summary>
    /// Återställ till standardvärden
    /// </summary>
    [RelayCommand]
    private async Task ResetAsync()
    {
        var result = await Application.Current.MainPage.DisplayAlert(
            "Återställ inställningar",
            $"Vill du återställa {SensorName} till standardvärden?\n\n" +
            $"Detta kommer att spara standardvärdena både lokalt och till ESP32 (om tillgängligt).",
            "Ja", "Avbryt");

        if (result)
        {
            try
            {
                IsLoading = true;

                // Sätt standardvärden
                SetDefaultValues();

                // Spara standardvärdena (använder samma logik som SaveAsync)
                var optimal = double.Parse(OptimalMin);
                var warning = double.Parse(WarningTemp);
                var critical = double.Parse(CriticalTemp);

                bool esp32Success = false;
                bool localSuccess = false;

                // Spara till ESP32 (om tillgängligt)
                if (!UseMockData)
                {
                    esp32Success = await SaveToEsp32Async(optimal, warning, critical);
                }

                // Spara till lokala settings
                localSuccess = await SaveToLocalSettingsAsync(optimal, warning, critical);

                // Visa resultat
                if (localSuccess)
                {
                    var statusText = esp32Success ? "både ESP32 och lokalt" : "lokalt (ESP32 ej tillgänglig)";
                    await Application.Current.MainPage.DisplayAlert(
                        "Återställt! 🔄",
                        $"Inställningar för {SensorName} återställda till standardvärden och sparade {statusText}.\n\n" +
                        $"🟢 Optimal: från {OptimalMin}°C\n" +
                        $"🟡 Varning: från {WarningTemp}°C\n" +
                        $"🔴 Kritisk: från {CriticalTemp}°C",
                        "OK");

                    UpdateConnectionStatus(esp32Success);
                }
                else
                {
                    await ShowErrorAsync("Kunde inte spara standardvärdena. Försök igen.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting settings: {ex.Message}");
                await ShowErrorAsync("Fel vid återställning. Försök igen.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    #endregion

    #region ESP32 Sync Commands
    /// <summary>
    /// Synkronisera från ESP32 (ladda om från ESP32)
    /// </summary>
    [RelayCommand]
    private async Task SyncFromEsp32Async()
    {
        if (UseMockData)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Ej tillgängligt",
                "ESP32-synkronisering är inte tillgänglig i testdata-läge.",
                "OK");
            return;
        }

        try
        {
            IsLoading = true;
            ConnectionStatus = "🔄 Synkroniserar från ESP32...";

            bool success = await LoadFromEsp32Async();

            if (success)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Synkroniserat! 🔄",
                    $"Inställningar för {SensorName} har uppdaterats från ESP32.\n\n" +
                    $"🟢 Optimal: från {OptimalMin}°C\n" +
                    $"🟡 Varning: från {WarningTemp}°C\n" +
                    $"🔴 Kritisk: från {CriticalTemp}°C",
                    "OK");

                ConnectionStatus = "✅ ESP32 - Synkroniserat";
            }
            else
            {
                await ShowErrorAsync("Kunde inte synkronisera från ESP32. Kontrollera anslutningen.");
                ConnectionStatus = "❌ ESP32 ej tillgänglig";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error syncing from ESP32: {ex.Message}");
            await ShowErrorAsync("Fel vid synkronisering från ESP32.");
            ConnectionStatus = "❌ Synkroniseringsfel";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Tvinga synkronisering till ESP32 (skicka lokala värden till ESP32)
    /// </summary>
    [RelayCommand]
    private async Task SyncToEsp32Async()
    {
        if (UseMockData)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Ej tillgängligt",
                "ESP32-synkronisering är inte tillgänglig i testdata-läge.",
                "OK");
            return;
        }

        var result = await Application.Current.MainPage.DisplayAlert(
            "Synkronisera till ESP32",
            $"Vill du skicka nuvarande inställningar till ESP32?\n\n" +
            $"🟢 Optimal: från {OptimalMin}°C\n" +
            $"🟡 Varning: från {WarningTemp}°C\n" +
            $"🔴 Kritisk: från {CriticalTemp}°C",
            "Ja", "Avbryt");

        if (!result)
            return;

        try
        {
            IsLoading = true;
            ConnectionStatus = "🔄 Synkroniserar till ESP32...";

            if (!ValidateTemperatureValues())
            {
                await ShowErrorAsync("Ogiltiga temperaturvärden. Kontrollera att: Optimal < Varning < Kritisk");
                return;
            }

            var optimal = double.Parse(OptimalMin);
            var warning = double.Parse(WarningTemp);
            var critical = double.Parse(CriticalTemp);

            bool success = await SaveToEsp32Async(optimal, warning, critical);

            if (success)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Synkroniserat! ✅",
                    $"Inställningar för {SensorName} har skickats till ESP32.",
                    "OK");

                ConnectionStatus = "✅ ESP32 - Synkroniserat";
            }
            else
            {
                await ShowErrorAsync("Kunde inte synkronisera till ESP32. Kontrollera anslutningen.");
                ConnectionStatus = "❌ ESP32 ej tillgänglig";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error syncing to ESP32: {ex.Message}");
            await ShowErrorAsync("Fel vid synkronisering till ESP32.");
            ConnectionStatus = "❌ Synkroniseringsfel";
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion

    #region Default Values & Validation 
    /// <summary>
    /// Sätt standardvärden baserat på sensortyp
    /// </summary>
    private void SetDefaultValues()
    {
        OptimalMin = GetDefaultOptimalMin();
        WarningTemp = GetDefaultWarningTemp();
        CriticalTemp = GetDefaultCriticalTemp();

        Debug.WriteLine($"🔧 Standardvärden för {SensorName}: {OptimalMin}° → {WarningTemp}° → {CriticalTemp}°");
    }

    /// <summary>
    /// Standardvärden för optimal temperatur
    /// </summary>
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

    /// <summary>
    /// Standardvärden för varningstemperatur
    /// </summary>
    private string GetDefaultWarningTemp()
    {
        return SensorName switch
        {
            "Kolv" => "80",      // Varning börjar vid 80°C
            "Destillat" => "85", // Varning börjar vid 85°C
            "Kylare" => "30",    // Varning börjar vid 30°C
            _ => "80"
        };
    }

    /// <summary>
    /// Standardvärden för kritisk temperatur
    /// </summary>
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
    /// Validering: optimal -- warning -- critical
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
    #endregion

    #region Helper Methods
    /// <summary>
    /// Visa felmeddelande
    /// </summary>
    private async Task ShowErrorAsync(string message)
    {
        await Application.Current.MainPage.DisplayAlert("Fel ❌", message, "OK");
    }

    /// <summary>
    /// Kontrollera om ESP32-integration är tillgänglig
    /// </summary>
    public bool CanSyncWithEsp32 => !UseMockData && !string.IsNullOrEmpty(SensorName);
    #endregion

}
