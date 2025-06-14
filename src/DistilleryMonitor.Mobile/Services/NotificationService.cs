using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;
using Microsoft.Maui.Authentication;
using Plugin.LocalNotification;
using System.Timers;

namespace DistilleryMonitor.Mobile.Services;

public interface IAppNotificationService
{
    Task ShowNotificationAsync(string title, string message, bool isCritical = false);
    Task CheckTemperatureWarnings(List<TemperatureReading> sensors);
    Task<bool> RequestPermissionAsync();

    // MONITORING
    void ReportDataReceived(); // Anropas från MainPageViewModel
    void StartSensorMonitoring(int timeoutSeconds = 5);
    void StopSensorMonitoring();
    void UpdateMonitoringSettings(int timeoutSeconds);
}

public class AppNotificationService : IAppNotificationService
{
    private readonly ApiService _apiService;
    private readonly ISettingsService _settingsService;
    private System.Timers.Timer _monitoringTimer;
    private DateTime _lastDataReceived = DateTime.Now;
    private int _timeoutSeconds = 5;
    private bool _isMonitoring = false;
    private bool _hasShownTimeoutWarning = false;

    // Håller koll på vilka varningar som redan visats
    private readonly HashSet<string> _shownWarnings = new HashSet<string>();
    private readonly HashSet<string> _shownCritical = new HashSet<string>();
    private readonly HashSet<string> _shownOptimal = new HashSet<string>();

    // CONSTRUCTOR
    public AppNotificationService(ApiService apiService, ISettingsService settingsService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
    }

    // Rapportera att data togs emot
    public void ReportDataReceived()
    {
        _lastDataReceived = DateTime.Now;
        _hasShownTimeoutWarning = false;
        System.Diagnostics.Debug.WriteLine($"📡 Data rapporterad: {DateTime.Now:HH:mm:ss}");
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fel vid begäran om notifikationstillstånd: {ex.Message}");
            return false;
        }
    }

    public async Task ShowNotificationAsync(string title, string message, bool isCritical = false)
    {
        try
        {
            // ANDROID NOTIFICATION
            var request = new NotificationRequest
            {
                NotificationId = DateTime.Now.Millisecond,
                Title = title,
                Description = message,
                BadgeNumber = 1
            };

            await LocalNotificationCenter.Current.Show(request);

            if (isCritical)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Vibration fel: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"✅ Notifikation skickad: {title}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Notifikationsfel: {ex.Message}");
        }
    }

    // STARTA SENSOR MONITORING
    public void StartSensorMonitoring(int timeoutSeconds = 5)
    {
        if (_isMonitoring)
            return;

        _timeoutSeconds = timeoutSeconds;
        _isMonitoring = true;
        _lastDataReceived = DateTime.Now;
        _hasShownTimeoutWarning = false;

        // Kontrollera varje sekund
        _monitoringTimer = new System.Timers.Timer(1000);
        _monitoringTimer.Elapsed += CheckSensorStatus;
        _monitoringTimer.Start();

        System.Diagnostics.Debug.WriteLine($"🔍 Sensor monitoring startad - timeout: {timeoutSeconds}s");
    }

    // STOPPA MONITORING
    public void StopSensorMonitoring()
    {
        _isMonitoring = false;
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
        System.Diagnostics.Debug.WriteLine("⏹️ Sensor monitoring stoppad");
    }

    // UPPDATERA INSTÄLLNINGAR
    public void UpdateMonitoringSettings(int timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
        System.Diagnostics.Debug.WriteLine($"⚙️ Sensor timeout uppdaterad: {timeoutSeconds}s");
    }

    // KONTROLLERA SENSOR STATUS MED MOCKDATA SUPPORT
    private async void CheckSensorStatus(object sender, ElapsedEventArgs e)
    {
        try
        {
            await CheckForTimeout();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Sensor monitoring fel: {ex.Message}");
        }
    }

    // KONTROLLERA TIMEOUT
    private async Task CheckForTimeout()
    {
        var timeSinceLastData = DateTime.Now - _lastDataReceived;

        if (timeSinceLastData.TotalSeconds >= _timeoutSeconds && !_hasShownTimeoutWarning)
        {
            _hasShownTimeoutWarning = true;

            await ShowNotificationAsync(
                "🚨 SENSOR VARNING",
                $"Ingen data från sensorer på {_timeoutSeconds} sekunder! Kontrollera din bryggning omedelbart!",
                true); // Kritisk notifikation

            System.Diagnostics.Debug.WriteLine("🚨 Sensor timeout notifikation skickad!");
        }
    }

    public async Task CheckTemperatureWarnings(List<TemperatureReading> sensors)
    {
        try
        {
            foreach (var sensor in sensors)
            {
                var settings = await GetSensorSettings(sensor.Id, sensor.Name);

                string warningKey = $"{sensor.Name}_Warning";
                string criticalKey = $"{sensor.Name}_Critical";
                string optimalKey = $"{sensor.Name}_Optimal";

                // Kolla Critical
                if (sensor.Temperature >= settings.Critical)
                {
                    if (!_shownCritical.Contains(criticalKey))
                    {
                        await ShowNotificationAsync("🚨 KRITISK TEMPERATUR!",
                            $"{sensor.Name}: {sensor.Temperature:F1}°C (Kritisk: {settings.Critical}°C)", true);
                        _shownCritical.Add(criticalKey);
                        System.Diagnostics.Debug.WriteLine($"🚨 KRITISK: {sensor.Name} = {sensor.Temperature:F1}°C");
                    }
                    // Rensa warning och optimal så de kan visas igen
                    _shownWarnings.Remove(warningKey);
                    _shownOptimal.Remove(optimalKey);
                }
                // Kolla Warning
                else if (sensor.Temperature >= settings.Warning)
                {
                    // VISA WARNING om:
                    // 1. Aldrig visat warning för denna sensor, ELLER
                    // 2. Sensorn var på critical och nu är tillbaka till warning
                    bool wasInCritical = _shownCritical.Contains(criticalKey);

                    if (!_shownWarnings.Contains(warningKey) || wasInCritical)
                    {
                        await ShowNotificationAsync("⚠️ Temperatur Varning",
                            $"{sensor.Name}: {sensor.Temperature:F1}°C (Varning: {settings.Warning}°C)", false);
                        _shownWarnings.Add(warningKey);

                        if (wasInCritical)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ VARNING: {sensor.Name} = {sensor.Temperature:F1}°C (från kritisk)");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ VARNING: {sensor.Name} = {sensor.Temperature:F1}°C (första gången)");
                        }
                    }

                    // Rensa critical och optimal
                    _shownCritical.Remove(criticalKey);
                    _shownOptimal.Remove(optimalKey);
                }
                // Kolla Optimal
                else if (sensor.Temperature >= settings.Optimal && sensor.Temperature < settings.Warning)
                {
                    bool wasInWarning = _shownWarnings.Contains(warningKey) || _shownCritical.Contains(criticalKey);

                    if (!_shownOptimal.Contains(optimalKey) || wasInWarning)
                    {
                        await ShowNotificationAsync("✅ Optimal Temperatur!",
                            $"{sensor.Name}: {sensor.Temperature:F1}°C - Perfekt för destillation!", false);
                        _shownOptimal.Add(optimalKey);
                    }

                    // Rensa varningar
                    _shownWarnings.Remove(warningKey);
                    _shownCritical.Remove(criticalKey);
                }
                else
                {
                    // Under optimal - rensa allt
                    _shownWarnings.Remove(warningKey);
                    _shownCritical.Remove(criticalKey);
                    _shownOptimal.Remove(optimalKey);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Temperatur-varning fel: {ex.Message}");
        }
    }

    // Helper metod för sensor-inställningar
    private async Task<(double Optimal, double Warning, double Critical)> GetSensorSettings(int sensorId, string sensorName)
    {
        try
        {
            return sensorName.ToLower() switch
            {
                "kolv" => (
                    await _settingsService.GetKolvOptimalMinAsync(),      // 70.0
                    await _settingsService.GetKolvWarningTempAsync(),     // 80.0  
                    await _settingsService.GetKolvCriticalTempAsync()     // 90.0
                ),
                "destillat" => (
                    await _settingsService.GetDestillatOptimalMinAsync(),  // 75.0
                    await _settingsService.GetDestillatWarningTempAsync(), // 85.0
                    await _settingsService.GetDestillatCriticalTempAsync() // 95.0
                ),
                "kylare" => (
                    await _settingsService.GetKylareOptimalMinAsync(),     // 20.0
                    await _settingsService.GetKylareWarningTempAsync(),    // 30.0
                    await _settingsService.GetKylareCriticalTempAsync()    // 40.0
                ),
                _ => (70.0, 80.0, 90.0) // Fallback
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Kunde inte hämta inställningar: {ex.Message}");
            return (70.0, 80.0, 90.0); // Fallback
        }
    }

}