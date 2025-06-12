using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;
using System.Diagnostics;
using Microsoft.Maui.Graphics;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile.ViewModels;

[QueryProperty(nameof(SensorId), "sensorId")]
public partial class TemperatureDetailViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ISettingsService _settingsService;
    private readonly MockDataService _mockDataService; // ✅ Nu via DI
    private Timer? _updateTimer;
    private const int UPDATE_INTERVAL_SECONDS = 3;
    public ISettingsService SettingsService => _settingsService;

    [ObservableProperty] private int sensorId = -1;
    [ObservableProperty] private string sensorName = string.Empty;
    [ObservableProperty] private double temperature;
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private string statusMessage = "Ingen information tillgänglig";
    [ObservableProperty] private string optimalRange = "--";
    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private DateTime lastUpdated;
    [ObservableProperty] private double optimalMin;
    [ObservableProperty] private double warningTemp;
    [ObservableProperty] private double criticalTemp;
    [ObservableProperty] private bool useMockData = false;
    [ObservableProperty] private string connectionStatus = "";

    // Ta emot MockDataService via DI
    public TemperatureDetailViewModel(ApiService apiService, ISettingsService settingsService, MockDataService mockDataService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _mockDataService = mockDataService; // Använd injected service
    }

    public string OptimalRangeNew => $"{OptimalMin:F1}°C - {WarningTemp:F1}°C";

    public string StatusNew
    {
        get
        {
            return Temperature switch
            {
                var t when t < OptimalMin => "För kallt",
                var t when t >= OptimalMin && t < WarningTemp => "Optimal",
                var t when t >= WarningTemp && t < CriticalTemp => "Varning",
                _ => "Kritisk"
            };
        }
    }

    public string StatusColorHex
    {
        get
        {
            return Temperature switch
            {
                var t when t < OptimalMin => "#0066cc",      // Blå
                var t when t >= OptimalMin && t < WarningTemp => "#28a745", // Grön
                var t when t >= WarningTemp && t < CriticalTemp => "#ffc107", // Gul
                _ => "#dc3545"  // Röd
            };
        }
    }

    public Color StatusColor
    {
        get
        {
            return Temperature switch
            {
                var t when t < OptimalMin => Color.FromArgb("#0066cc"),      // Blå
                var t when t >= OptimalMin && t < WarningTemp => Color.FromArgb("#28a745"), // Grön
                var t when t >= WarningTemp && t < CriticalTemp => Color.FromArgb("#ffc107"), // Gul
                _ => Color.FromArgb("#dc3545")  // Röd
            };
        }
    }

    /// <summary>
    /// Körs när SensorId ändras (från navigation)
    /// </summary>
    partial void OnSensorIdChanged(int value)
    {
        Debug.WriteLine($"TemperatureDetailViewModel: SensorId changed to {value}");
        if (value >= 0)
        {
            _ = Task.Run(async () =>
            {
                await LoadSettingsAsync(); // ← Ladda settings först
                await LoadSensorDataAsync();
                await MainThread.InvokeOnMainThreadAsync(() => StartAutoUpdate());
            });
        }
    }

    /// <summary>
    /// Ladda inställningar från Settings
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            UseMockData = await _settingsService.GetUseMockDataAsync();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ConnectionStatus = UseMockData ? "🧪 Testdata" : "🔌 ESP32";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Laddar sensordata baserat på SensorId
    /// </summary>
    [RelayCommand]
    private async Task LoadSensorDataAsync()
    {
        try
        {
            IsLoading = true;

            // Ladda senaste settings
            await LoadSettingsAsync();

            TemperatureResponse? response = null;

            if (UseMockData)
            {
                // ✅ MOCK DATA MODE - Nu med Settings integration!
                StatusMessage = "Laddar testdata...";
                response = await _mockDataService.GetTemperaturesAsync();
                ConnectionStatus = "🧪 Testdata";
            }
            else
            {
                // ✅ REAL ESP32 MODE
                StatusMessage = "Hämtar från ESP32...";
                response = await _apiService.GetTemperaturesAsync();
                if (response != null)
                {
                    ConnectionStatus = "✅ ESP32 Live";
                }
                else
                {
                    ConnectionStatus = "❌ Ingen anslutning";
                    StatusMessage = "Ingen anslutning till ESP32";
                    return; // Avbryt om ingen data
                }
            }

            // Hitta rätt sensor
            if (response?.Sensors != null)
            {
                var sensor = response.Sensors.FirstOrDefault(s => s.Id == SensorId);
                if (sensor != null)
                {
                    await UpdateSensorData(sensor); // ✅ Nu async för Settings
                }
                else
                {
                    StatusMessage = $"Sensor med ID {SensorId} hittades inte";
                    ConnectionStatus += " - Sensor saknas";
                }
            }
            else
            {
                StatusMessage = "Ingen sensordata tillgänglig";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fel vid hämtning: {ex.Message}";
            ConnectionStatus = "⚠️ Anslutningsfel";
            Debug.WriteLine($"Error loading sensor data: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Uppdaterar UI med sensordata
    /// </summary>
    private async Task UpdateSensorData(TemperatureReading sensor) 
    {
        SensorName = sensor.Name;
        Temperature = sensor.Temperature;
        Status = sensor.Status.ToUpper();
        StatusMessage = GetStatusMessage(sensor.Status, sensor.Temperature);

        // Ladda temperaturlimits från Settings
        await SetTemperatureLimitsFromSettings(sensor.Name);

        LastUpdated = DateTime.Now;

        // Uppdatera connection status med sensor info
        var dataSource = UseMockData ? "🧪 Testdata" : "✅ ESP32 Live";
        ConnectionStatus = $"{dataSource} - {LastUpdated:HH:mm:ss}";

        // Trigga alla computed properties
        OnPropertyChanged(nameof(OptimalRangeNew));
        OnPropertyChanged(nameof(StatusNew));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(StatusColorHex));
    }

    /// <summary>
    /// Sätter temperaturvärden från Settings baserat på sensortyp
    /// </summary>
    private async Task SetTemperatureLimitsFromSettings(string sensorName)
    {
        try
        {
            switch (sensorName)
            {
                case "Kolv":
                    OptimalMin = await _settingsService.GetKolvOptimalMinAsync();
                    WarningTemp = await _settingsService.GetKolvWarningTempAsync();
                    CriticalTemp = await _settingsService.GetKolvCriticalTempAsync();
                    break;

                case "Destillat":
                    OptimalMin = await _settingsService.GetDestillatOptimalMinAsync();
                    WarningTemp = await _settingsService.GetDestillatWarningTempAsync();
                    CriticalTemp = await _settingsService.GetDestillatCriticalTempAsync();
                    break;

                case "Kylare":
                    OptimalMin = await _settingsService.GetKylareOptimalMinAsync();
                    WarningTemp = await _settingsService.GetKylareWarningTempAsync();
                    CriticalTemp = await _settingsService.GetKylareCriticalTempAsync();
                    break;

                default:
                    // Fallback till Kolv-värden
                    OptimalMin = await _settingsService.GetKolvOptimalMinAsync();
                    WarningTemp = await _settingsService.GetKolvWarningTempAsync();
                    CriticalTemp = await _settingsService.GetKolvCriticalTempAsync();
                    break;
            }

            // Trigga uppdatering av OptimalRangeNew
            OnPropertyChanged(nameof(OptimalRangeNew));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading temperature limits from settings: {ex.Message}");

            // Fallback till hårdkodade värden om Settings misslyckas
            SetTemperatureLimitsHardcoded(sensorName);
        }
    }

    /// <summary>
    /// Fallback med hårdkodade värden om Settings misslyckas
    /// </summary>
    private void SetTemperatureLimitsHardcoded(string sensorName)
    {
        switch (sensorName)
        {
            case "Kolv":
                OptimalMin = 70.0;
                WarningTemp = 80.0;
                CriticalTemp = 90.0;
                break;
            case "Destillat":
                OptimalMin = 75.0;
                WarningTemp = 85.0;
                CriticalTemp = 95.0;
                break;
            case "Kylare":
                OptimalMin = 20.0;
                WarningTemp = 30.0;
                CriticalTemp = 40.0;
                break;
            default:
                OptimalMin = 50.0;
                WarningTemp = 80.0;
                CriticalTemp = 90.0;
                break;
        }

        // Trigga uppdatering av OptimalRangeNew
        OnPropertyChanged(nameof(OptimalRangeNew));
    }

    /// <summary>
    /// Genererar statusmeddelande baserat på status och temperatur
    /// </summary>
    private string GetStatusMessage(string status, double temperature)
    {
        return status switch
        {
            "optimal" => $"Temperaturen är optimal för {SensorName.ToLower()}",
            "warning" => $"Varning: Temperaturen är {temperature:F1}°C - kontrollera processen",
            "cold" => $"Temperaturen är låg ({temperature:F1}°C) - vänta på uppvärmning",
            "hot" => $"Temperaturen är hög ({temperature:F1}°C) - kontrollera kylning",
            _ => "Status okänd - kontrollera sensoranslutning"
        };
    }

    /// <summary>
    /// Manuell uppdatering
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSensorDataAsync();
    }

    /// <summary>
    /// Visa inställningar (placeholder)
    /// </summary>
    [RelayCommand]
    private async Task SettingsAsync()
    {
        // Navigation till sensor-inställningar med både ID och namn
        await Shell.Current.GoToAsync($"sensor-settings?sensorId={SensorId}&sensorName={Uri.EscapeDataString(SensorName)}");
    }

    /// <summary>
    /// Startar automatisk uppdatering
    /// </summary>
    public void StartAutoUpdate()
    {
        StopAutoUpdate();

        // Hämta uppdateringsintervall från settings
        _ = Task.Run(async () =>
        {
            var updateInterval = await _settingsService.GetUpdateIntervalAsync();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _updateTimer = new Timer(async _ =>
                {
                    await LoadSensorDataAsync();
                }, null, TimeSpan.FromSeconds(updateInterval), TimeSpan.FromSeconds(updateInterval));
            });
        });
    }

    /// <summary>
    /// Stoppar automatisk uppdatering
    /// </summary>
    public void StopAutoUpdate()
    {
        _updateTimer?.Dispose();
        _updateTimer = null;
    }

    /// <summary>
    /// Körs när sidan visas igen (efter navigation tillbaka)
    /// </summary>
    public async Task OnAppearingAsync()
    {
        Debug.WriteLine($"TemperatureDetailViewModel: OnAppearing for sensor {SensorId}");

        // Starta om timer om den inte körs
        if (_updateTimer == null)
        {
            StartAutoUpdate();
        }

        // Ladda data direkt för snabb uppdatering
        await LoadSensorDataAsync();
    }

    /// <summary>
    /// Körs när sidan försvinner (navigation bort)
    /// </summary>
    public void OnDisappearing()
    {
        Debug.WriteLine($"TemperatureDetailViewModel: OnDisappearing for sensor {SensorId}");

        // Stoppa timer för att spara batteri/resurser
        StopAutoUpdate();
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    public void Dispose()
    {
        Debug.WriteLine($"TemperatureDetailViewModel: Disposing for sensor {SensorId}");
        StopAutoUpdate();
    }

}
