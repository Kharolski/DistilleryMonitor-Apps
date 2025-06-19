using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;
using Microsoft.Maui.Authentication;
using Plugin.LocalNotification;
using System.Timers;

#if ANDROID
using Android.OS;
#endif

namespace DistilleryMonitor.Mobile.Services;

#region Interface
public interface IAppNotificationService
{
    // NOTIFICATIONS
    Task ShowNotificationAsync(string title, string message, bool isCritical = false);
    Task CheckTemperatureWarnings(List<TemperatureReading> sensors);

    // PERMISSIONS
    Task<bool> RequestPermissionAsync();
    Task<bool> HasPermissionAsync(); 
    Task OpenAppSettingsAsync();

    // MONITORING
    void ReportDataReceived();
    void StartSensorMonitoring(int timeoutSeconds = 5);
    void StopSensorMonitoring();
    void UpdateMonitoringSettings(int timeoutSeconds);
}
#endregion

#region Implementation
public class AppNotificationService : IAppNotificationService
{
    #region Fields & Services
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
    #endregion

    #region Constructor
    public AppNotificationService(ApiService apiService, ISettingsService settingsService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
    }
    #endregion

    #region Permission Management
    /// <summary>
    /// Kolla om notifikations-permission redan finns
    /// </summary>
    public async Task<bool> HasPermissionAsync()
    {
        try
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                // ANVÄND MAUI PERMISSIONS FÖR ANDROID 13+
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                bool hasPermission = status == PermissionStatus.Granted;
                return hasPermission;
            }
            else
            {
                // ÄLDRE ANDROID - ANVÄND PLUGIN
                bool enabled = await LocalNotificationCenter.Current.AreNotificationsEnabled();
                return enabled;
            }
#else
        // iOS eller andra plattformar
        return await LocalNotificationCenter.Current.AreNotificationsEnabled();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Permission check fel: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Begär notifikations-permission från användaren
    /// </summary>
    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var currentStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                
                if (currentStatus == PermissionStatus.Granted)
                {
                    return true;
                }

                if (currentStatus == PermissionStatus.Denied)
                {
                    var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    bool granted = status == PermissionStatus.Granted;

                    System.Diagnostics.Debug.WriteLine($"🔔 MAUI Permission result: {granted}");
                    return granted;
                }

                if (currentStatus == PermissionStatus.Disabled)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Permission permanently denied");
                    return false;
                }
            }

            // Fallback för äldre Android
            var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
            return result;
#else
        var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
        return result;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Fel vid begäran om notifikationstillstånd: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Öppna app-inställningar för manuell aktivering
    /// </summary>
    public async Task OpenAppSettingsAsync()
    {
        try
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.SetData(Android.Net.Uri.Parse($"package:{Platform.CurrentActivity?.PackageName}"));
            Platform.CurrentActivity?.StartActivity(intent);
#endif
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Kunde inte öppna app-inställningar: {ex.Message}");
        }
    }

    #endregion

    #region Data Monitoring
    /// <summary>
    /// Rapportera att data togs emot
    /// </summary>
    public void ReportDataReceived()
    {
        _lastDataReceived = DateTime.Now;
        _hasShownTimeoutWarning = false;
    }

    /// <summary>
    /// Starta sensor monitoring
    /// </summary>
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

    /// <summary>
    /// Stoppa monitoring
    /// </summary>
    public void StopSensorMonitoring()
    {
        _isMonitoring = false;
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
    }

    /// <summary>
    /// Uppdatera monitoring inställningar
    /// </summary>
    public void UpdateMonitoringSettings(int timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Kontrollera sensor status
    /// </summary>
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

    /// <summary>
    /// Kontrollera timeout
    /// </summary>
    private async Task CheckForTimeout()
    {
        var timeSinceLastData = DateTime.Now - _lastDataReceived;
        if (timeSinceLastData.TotalSeconds >= _timeoutSeconds && !_hasShownTimeoutWarning)
        {
            _hasShownTimeoutWarning = true;
            await ShowNotificationAsync(
                "🚨 SENSOR VARNING",
                $"Ingen data från sensorer på {_timeoutSeconds} sekunder! Kontrollera din bryggning omedelbart!",
                true);
            System.Diagnostics.Debug.WriteLine("🚨 Sensor timeout notifikation skickad!");
        }
    }
    #endregion

    #region Notifications
    /// <summary>
    /// Visa notifikation
    /// </summary>
    public async Task ShowNotificationAsync(string title, string message, bool isCritical = false)
    {
        try
        {
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

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Notifikationsfel: {ex.Message}");
        }
    }
    #endregion

    #region Temperature Warnings
    /// <summary>
    /// Kontrollera temperatur-varningar
    /// </summary>
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
                    _shownWarnings.Remove(warningKey);
                    _shownOptimal.Remove(optimalKey);
                }
                // Kolla Warning
                else if (sensor.Temperature >= settings.Warning)
                {
                    bool wasInCritical = _shownCritical.Contains(criticalKey);
                    if (!_shownWarnings.Contains(warningKey) || wasInCritical)
                    {
                        await ShowNotificationAsync("⚠️ Temperatur Varning",
                            $"{sensor.Name}: {sensor.Temperature:F1}°C (Varning: {settings.Warning}°C)", false);
                        _shownWarnings.Add(warningKey);
                        System.Diagnostics.Debug.WriteLine($"⚠️ VARNING: {sensor.Name} = {sensor.Temperature:F1}°C");
                    }
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

    /// <summary>
    /// Hämta sensor-inställningar
    /// </summary>
    private async Task<(double Optimal, double Warning, double Critical)> GetSensorSettings(int sensorId, string sensorName)
    {
        try
        {
            return sensorName.ToLower() switch
            {
                "kolv" => (
                    await _settingsService.GetKolvOptimalMinAsync(),
                    await _settingsService.GetKolvWarningTempAsync(),
                    await _settingsService.GetKolvCriticalTempAsync()
                ),
                "destillat" => (
                    await _settingsService.GetDestillatOptimalMinAsync(),
                    await _settingsService.GetDestillatWarningTempAsync(),
                    await _settingsService.GetDestillatCriticalTempAsync()
                ),
                "kylare" => (
                    await _settingsService.GetKylareOptimalMinAsync(),
                    await _settingsService.GetKylareWarningTempAsync(),
                    await _settingsService.GetKylareCriticalTempAsync()
                ),
                _ => (70.0, 80.0, 90.0)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Kunde inte hämta inställningar: {ex.Message}");
            return (70.0, 80.0, 90.0);
        }
    }
    #endregion
}
#endregion
