using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Core.Models;
using System.Collections.ObjectModel;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    #region Fields & Services

    private readonly ApiService _apiService;
    private readonly ISettingsService _settingsService;
    private readonly MockDataService _mockDataService;
    private readonly IAppNotificationService _notificationService;
    private readonly IDatabaseService _databaseService;
    private Timer? _updateTimer;

    #endregion

    #region Observable Properties

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool isConnected = false;
    [ObservableProperty] private string connectionStatus = "Ansluter...";
    [ObservableProperty] private ObservableCollection<TemperatureReading> sensors = new();
    [ObservableProperty] private bool useMockData = false;
    [ObservableProperty] private List<TemperatureHistory> historyData = new();

    #endregion

    #region Constructor & Initialization

    public MainPageViewModel(ApiService apiService, ISettingsService settingsService,
                          MockDataService mockDataService, IAppNotificationService notificationService,
                          IDatabaseService databaseService) 
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _mockDataService = mockDataService;
        _notificationService = notificationService;
        _databaseService = databaseService; 

        // Ladda mock data setting
        _ = Task.Run(async () => await LoadSettingsAsync());

        // Fråga om notifikationer vid första start
        _ = Task.Run(async () => await CheckNotificationPermissionOnStartup());

        // Initiera databas 
        _ = Task.Run(async () => await InitializeDatabaseAsync());
    }

    /// <summary>
    /// Initiera databas vid app-start
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();

            // Kolla om vi ska fråga om ny process
            bool shouldPrompt = await _databaseService.ShouldPromptForNewProcessAsync();
            if (shouldPrompt)
            {
                var lastData = await _databaseService.GetLastDataTimestampAsync();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    bool startNew = await Application.Current.MainPage.DisplayAlert(
                        "🆕 Ny Destillation?",
                        $"Senaste data är från {lastData:yyyy-MM-dd HH:mm}.\n\nVill du starta en ny destillationsprocess?\n(Detta raderar all gammal historik)",
                        "Ja, starta ny",
                        "Nej, fortsätt"
                    );

                    if (startNew)
                    {
                        await _databaseService.ClearAllHistoryAsync();
                        await Application.Current.MainPage.DisplayAlert("✅", "Historik rensad! Redo för ny destillation.", "OK");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Databas init fel: {ex.Message}");
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
                if (UseMockData)
                {
                    //ConnectionStatus = "🧪 Testdata Mode";
                }
                else
                {
                    //ConnectionStatus = "Ansluter...";
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Kontrollerar och konfigurerar notifikationstillstånd vid appstart.
    /// Hanterar både Android-systemtillstånd och app-interna inställningar.
    /// Guidar användaren genom aktiveringsprocessen vid första användning.
    /// </summary>
    private async Task CheckNotificationPermissionOnStartup()
    {
        try
        {
            await Task.Delay(2000);

            // 1. Kolla Mobilens - permission först
            bool hasSystemPermission = await _notificationService.HasPermissionAsync();
            System.Diagnostics.Debug.WriteLine($"🔔 System permission status: {hasSystemPermission}");

            if (hasSystemPermission)
            {
                // Mobil - permission = ON
                bool appNotificationsEnabled = await _settingsService.GetNotificationsEnabledAsync();

                if (appNotificationsEnabled)
                {
                    // App - Notis = ON → Gör ingenting
                }
                else
                {
                    // App - notifikation = OFF → Fråga om aktivering
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        bool userWantsToEnable = await Application.Current.MainPage.DisplayAlert(
                            "🔔 Aktivera notifikationer?",
                            "Notifikationer är tillåtna på din telefon men avstängda i appen.\n\n" +
                            "Vill du aktivera notifikationer för att få temperaturvarningar?",
                            "Ja, aktivera",
                            "Nej tack"
                        );

                        if (userWantsToEnable)
                        {
                            // JA → Aktivera app-notis + spara
                            await _settingsService.SetNotificationsEnabledAsync(true);
                            System.Diagnostics.Debug.WriteLine("✅ App-notifikationer aktiverade av användaren");

                            await Application.Current.MainPage.DisplayAlert(
                                "✅ Notifikationer aktiverade!",
                                "Du kommer nu få varningar vid kritiska temperaturer.",
                                "OK"
                            );

                        }
                        else
                        {
                            // NEJ → Tillbaka till main (gör ingenting)
                            System.Diagnostics.Debug.WriteLine("📱 Användaren valde att inte aktivera app-notifikationer");
                        }
                    });
                }
            }
            else
            {
                // Mobil - permission = OFF 
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    bool userWantsNotifications = await Application.Current.MainPage.DisplayAlert(
                        "🔔 Temperaturvarningar",
                        "För säker destillation behöver appen skicka temperaturvarningar.\n\n" +
                        "Android kommer fråga om tillåtelse för notifikationer.\n" +
                        "⚠️ VIKTIGT: Tryck 'Tillåt' för att få varningar!\n\n" +
                        "Vill du också aktivera notifikationer i appen?",
                        "Ja, aktivera allt",
                        "Bara Android-tillåtelse"
                    );

                    // Begär permission oavsett val
                    bool permissionGranted = await _notificationService.RequestPermissionAsync();
                    System.Diagnostics.Debug.WriteLine($"🔔 Permission result: {permissionGranted}");

                    if (permissionGranted)
                    {
                        // Mobil - permission godkänd
                        if (userWantsNotifications)
                        {
                            // JA → Aktivera både mobil och app
                            await _settingsService.SetNotificationsEnabledAsync(true);
                            System.Diagnostics.Debug.WriteLine("✅ Både mobil och app-notifikationer aktiverade");

                            await Application.Current.MainPage.DisplayAlert(
                                "✅ Notifikationer aktiverade!",
                                "Du kommer nu få varningar vid kritiska temperaturer.",
                                "OK"
                            );

                        }
                        else
                        {
                            // NEJ → Bara mobil-permission, app avstängd
                            await _settingsService.SetNotificationsEnabledAsync(false);
                            System.Diagnostics.Debug.WriteLine("📱 Mobil-permission aktiverad, app-notifikationer avstängda");

                            await Application.Current.MainPage.DisplayAlert(
                                "📱 Permission aktiverad",
                                "Mobil-permission är aktiverad. Du kan aktivera notifikationer senare i inställningar.",
                                "OK"
                            );
                        }
                    }
                    else
                    {
                        // ❌ Mobil - permission nekad
                        await _settingsService.SetNotificationsEnabledAsync(false);
                        System.Diagnostics.Debug.WriteLine("❌ Mobil-permission nekad");

                        // Erbjud manuell aktivering
                        bool openSettings = await Application.Current.MainPage.DisplayAlert(
                            "⚠️ Permission nekad",
                            "Notifikationer blockerades. Vill du öppna inställningar för att aktivera dem manuellt?",
                            "Ja, öppna inställningar",
                            "Nej, hoppa över"
                        );

                        if (openSettings)
                        {
                            await _notificationService.OpenAppSettingsAsync();
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Startup permission check: {ex.Message}");
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Manuell uppdatering för knappen - visar loading
    /// </summary>
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            await LoadSettingsAsync();

            if (UseMockData)
            {
                ConnectionStatus = "Uppdaterar testdata...";
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
                    ConnectionStatus = $"Live 🌡️ - {response.SensorCount} sensorer";
                }
                else
                {
                    IsConnected = false;
                }
            }

            await LoadHistoryDataAsync();
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
            await LoadSettingsAsync();

            if (UseMockData)
            {
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
                        ConnectionStatus = $"Live 🌡️ - {response.SensorCount} sensorer";
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

            await LoadHistoryDataAsync();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsConnected = false;
                ConnectionStatus = "Anslutningsfel";
                if (!UseMockData)
                {
                    Sensors.Clear();
                }
            });
            Console.WriteLine($"LoadTemperaturesAsync error: {ex}");
        }
    }

    #endregion

    #region Data Updates & Database

    /// <summary>
    /// Uppdatering av sensorer - förhindrar index-fel + sparar i databas
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
                        var index = Sensors.IndexOf(existingSensor);
                        if (index >= 0 && index < Sensors.Count)
                        {
                            Sensors[index] = newSensor;
                        }
                        else
                        {
                            Sensors.Remove(existingSensor);
                            Sensors.Add(newSensor);
                        }
                    }
                    else
                    {
                        Sensors.Add(newSensor);
                    }
                }

                var sensorsToRemove = Sensors.Where(s => !newSensors.Any(rs => rs.Id == s.Id)).ToList();
                foreach (var sensorToRemove in sensorsToRemove)
                {
                    if (Sensors.Contains(sensorToRemove))
                    {
                        Sensors.Remove(sensorToRemove);
                    }
                }
            });

            // sparar i databas (background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _databaseService.SaveTemperaturesAsync(newSensors);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Databas-sparning fel: {ex.Message}");
                }
            });

            // Kolla notifikations - inställningar först
            bool notificationsEnabled = await _settingsService.GetNotificationsEnabledAsync();
            if (notificationsEnabled)
            {
                _notificationService.ReportDataReceived();
                await _notificationService.CheckTemperatureWarnings(newSensors);
            }
            else
            {
                // Notifikationer avstängda
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating sensors: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Sensors.Clear();
                foreach (var sensor in newSensors)
                {
                    Sensors.Add(sensor);
                }
            });
            _notificationService.ReportDataReceived();
        }
    }

    /// <summary>
    /// Ladda historisk data för grafen
    /// </summary>
    private async Task LoadHistoryDataAsync()
    {
        try
        {
            var historyData = await _databaseService.GetRecentHistoryAsync(120); // 2 timmar

            var filteredData = FilterToInterval(historyData, TimeSpan.FromMinutes(2));

            HistoryData = filteredData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Fel vid laddning av historik: {ex.Message}");
            HistoryData = new List<TemperatureHistory>();
        }
    }

    /// <summary>
    /// Filtrera data till specificerat intervall
    /// </summary>
    private List<TemperatureHistory> FilterToInterval(List<TemperatureHistory> data, TimeSpan interval)
    {
        if (data == null || !data.Any())
            return new List<TemperatureHistory>();

        var result = new List<TemperatureHistory>();

        // Gruppera per sensor först
        var sensorGroups = data.GroupBy(d => d.SensorName);

        foreach (var sensorGroup in sensorGroups)
        {
            var sensorData = sensorGroup.OrderBy(d => d.Timestamp).ToList();
            var sensorName = sensorGroup.Key;

            if (!sensorData.Any())
                continue;

            var startTime = sensorData.First().Timestamp;
            var lastAddedTime = DateTime.MinValue;

            foreach (var item in sensorData)
            {
                // Lägg till första posten
                if (lastAddedTime == DateTime.MinValue)
                {
                    result.Add(item);
                    lastAddedTime = item.Timestamp;
                    continue;
                }

                // Lägg till om tillräckligt tid har gått
                if (item.Timestamp - lastAddedTime >= interval)
                {
                    result.Add(item);
                    lastAddedTime = item.Timestamp;
                }
            }

            // Lägg alltid till sista posten för varje sensor
            var lastItem = sensorData.Last();
            if (lastItem.Timestamp != lastAddedTime)
            {
                result.Add(lastItem);
            }
        }
        return result.OrderBy(r => r.Timestamp).ToList();
    }
    #endregion

    #region Auto Update Timer

    /// <summary>
    /// Startar automatisk uppdatering med dynamiskt intervall
    /// </summary>
    public async Task StartAutoUpdateAsync()
    {
        try
        {
            StopAutoUpdate();
            await LoadTemperaturesAsync();
            var updateInterval = await _settingsService.GetUpdateIntervalAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _updateTimer = new Timer(async _ =>
                {
                    try
                    {
                        var currentInterval = await _settingsService.GetUpdateIntervalAsync();
                        await LoadTemperaturesAsync();

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

    #endregion

    #region Cleanup

    public void Dispose()
    {
        StopAutoUpdate();
    }

    #endregion
}
