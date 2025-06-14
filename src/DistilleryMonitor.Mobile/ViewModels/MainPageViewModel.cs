using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Core.Models;
using System.Collections.ObjectModel;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ISettingsService _settingsService;
    private readonly MockDataService _mockDataService;
    private readonly IAppNotificationService _notificationService;
    private Timer? _updateTimer;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool isConnected = false;
    [ObservableProperty] private string connectionStatus = "Ansluter...";
    [ObservableProperty] private ObservableCollection<TemperatureReading> sensors = new();
    [ObservableProperty] private bool useMockData = false;

    // Ta emot MockDataService via DI 
    public MainPageViewModel(ApiService apiService, ISettingsService settingsService,
                          MockDataService mockDataService, IAppNotificationService notificationService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _mockDataService = mockDataService;
        _notificationService = notificationService; 

        // Ladda mock data setting
        _ = Task.Run(async () => await LoadSettingsAsync());

        // Fråga om notifikationer vid första start
        _ = Task.Run(async () => await CheckNotificationPermissionOnStartup());
    }

    /// <summary>
    /// Ladda inställningar från Settings
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            UseMockData = await _settingsService.GetUseMockDataAsync();

            // Uppdatera connection status baserat på mock data setting
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (UseMockData)
                {
                    //ConnectionStatus = "🧪 Testdata Mode";
                }
                else
                {
                    ConnectionStatus = "Ansluter...";
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    private async Task CheckNotificationPermissionOnStartup()
    {
        try
        {
            await Task.Delay(2000); // Vänta 2 sek så appen hinner ladda

            bool hasPermission = await _notificationService.RequestPermissionAsync();

            if (!hasPermission)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    bool userWants = await Application.Current.MainPage.DisplayAlert(
                        "🔔 Notifikationer",
                        "Vill du få varningar när temperaturer blir för höga?\n\nDetta hjälper dig övervaka destillationen säkert.",
                        "Ja, aktivera",
                        "Nej tack"
                    );

                    if (userWants)
                    {
                        await _notificationService.RequestPermissionAsync();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Startup permission check: {ex.Message}");
        }
    }

    /// <summary>
    /// Manuell uppdatering för knappen - visar loading
    /// </summary>
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;

            // Ladda senaste inställningar först
            await LoadSettingsAsync();

            if (UseMockData)
            {
                ConnectionStatus = "Uppdaterar testdata...";

                // Använder MockDataService Settings!
                var response = await _mockDataService.GetTemperaturesAsync();
                if (response != null)
                {
                    Sensors.Clear();
                    foreach (var sensor in response.Sensors)
                    {
                        Sensors.Add(sensor);
                    }
                    IsConnected = true;
                    ConnectionStatus = $"Testdata Mode - {response.SensorCount} sensorer";
                }
            }
            else
            {
                ConnectionStatus = "Ansluter till ESP32...";
                var response = await _apiService.GetTemperaturesAsync();
                if (response != null)
                {
                    Sensors.Clear();
                    foreach (var sensor in response.Sensors)
                    {
                        Sensors.Add(sensor);
                    }
                    IsConnected = true;
                    ConnectionStatus = $"Live - {response.SensorCount} sensorer";
                }
                else
                {
                    IsConnected = false;
                    //ConnectionStatus = "Ingen anslutning";
                }
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Anslutningsfel";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTemperaturesAsync()
    {
        try
        {
            // Ladda inställningar först
            await LoadSettingsAsync();

            if (UseMockData)
            {
                // MOCK DATA MODE - Nu med Settings integration!
                if (!IsConnected)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ConnectionStatus = "Laddar testdata...";
                    });
                }

                var response = await _mockDataService.GetTemperaturesAsync();
                if (response != null)
                {
                    await UpdateSensors(response.Sensors);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsConnected = true;
                        ConnectionStatus = $"Testdata Mode - {response.SensorCount} sensorer";
                    });
                }
            }
            else
            {
                // REAL ESP32 MODE
                if (!IsConnected)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ConnectionStatus = "Ansluter...";
                    });
                }

                var response = await _apiService.GetTemperaturesAsync();
                if (response != null)
                {
                    await UpdateSensors(response.Sensors);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsConnected = true;
                        ConnectionStatus = $"Live - {response.SensorCount} sensorer";
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsConnected = false;
                        ConnectionStatus = "Ingen anslutning";
                        Sensors.Clear();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsConnected = false;
                ConnectionStatus = "Anslutningsfel";

                // Rensa sensorer om inte mock data
                if (!UseMockData)
                {
                    Sensors.Clear();
                }
            });

            Console.WriteLine($"LoadTemperaturesAsync error: {ex}");
        }
    }

    /// <summary>
    /// Uppdatering av sensorer - förhindrar index-fel
    /// </summary>
    private async Task UpdateSensors(List<TemperatureReading> newSensors)
    {
        try
        {
            // Kör på main thread för UI-säkerhet
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Uppdatera befintliga sensorer
                foreach (var newSensor in newSensors)
                {
                    var existingSensor = Sensors.FirstOrDefault(s => s.Id == newSensor.Id);
                    if (existingSensor != null)
                    {
                        // SÄKER uppdatering - kontrollera att index fortfarande finns
                        var index = Sensors.IndexOf(existingSensor);
                        if (index >= 0 && index < Sensors.Count)
                        {
                            Sensors[index] = newSensor;
                        }
                        else
                        {
                            // Index inte giltigt - ta bort och lägg till igen
                            Sensors.Remove(existingSensor);
                            Sensors.Add(newSensor);
                        }
                    }
                    else
                    {
                        // Ny sensor
                        Sensors.Add(newSensor);
                    }
                }

                // Ta bort sensorer som inte längre finns
                var sensorsToRemove = Sensors.Where(s => !newSensors.Any(rs => rs.Id == s.Id)).ToList();
                foreach (var sensorToRemove in sensorsToRemove)
                {
                    if (Sensors.Contains(sensorToRemove))
                    {
                        Sensors.Remove(sensorToRemove);
                    }
                }
            });

            // RAPPORTERA ATT DATA TOGS EMOT - FUNGERAR FÖR BÅDE MOCKDATA OCH ESP32
            _notificationService.ReportDataReceived();

            // LÄGG TILL TEMPERATUR-VARNINGAR
            await _notificationService.CheckTemperatureWarnings(newSensors);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating sensors: {ex.Message}");

            // Fallback - rensa och lägg till alla igen
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Sensors.Clear();
                foreach (var sensor in newSensors)
                {
                    Sensors.Add(sensor);
                }
            });

            // ÄVEN VID FALLBACK - RAPPORTERA ATT DATA TOGS EMOT
            _notificationService.ReportDataReceived();
        }
    }

    /// <summary>
    /// Startar automatisk uppdatering med dynamiskt intervall
    /// </summary>
    public async Task StartAutoUpdateAsync()
    {
        try
        {
            // Stoppa befintlig timer först
            StopAutoUpdate();

            // Ladda data första gången
            await LoadTemperaturesAsync();

            // Hämta uppdateringsintervall från settings
            var updateInterval = await _settingsService.GetUpdateIntervalAsync();
            System.Diagnostics.Debug.WriteLine($"🔄 Startar timer med intervall: {updateInterval} sekunder");

            // Starta timer på main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _updateTimer = new Timer(async _ =>
                {
                    try
                    {
                        // Läsa nya inställningar varje uppdatering
                        var currentInterval = await _settingsService.GetUpdateIntervalAsync();
                        await LoadTemperaturesAsync();

                        // Om intervall ändrats - starta om timer
                        if (currentInterval != updateInterval)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔄 Intervall ändrat från {updateInterval}s till {currentInterval}s - startar om timer");
                            _ = Task.Run(async () => await StartAutoUpdateAsync());
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Timer update error: {ex}");
                    }
                }, null, TimeSpan.FromSeconds(updateInterval), TimeSpan.FromSeconds(updateInterval));
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartAutoUpdateAsync error: {ex}");
        }
    }

    public void StopAutoUpdate()
    {
        try
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StopAutoUpdate error: {ex}");
        }
    }

    public void Dispose()
    {
        StopAutoUpdate();
    }
}
