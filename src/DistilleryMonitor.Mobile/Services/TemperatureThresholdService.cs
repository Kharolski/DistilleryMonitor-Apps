using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;

namespace DistilleryMonitor.Mobile.Services
{
    public class TemperatureThresholdService
    {
        private readonly ISettingsService _settingsService;
        private Dictionary<string, TemperatureThresholds> _cachedThresholds;
        private readonly object _lock = new object();

        public TemperatureThresholdService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _cachedThresholds = new Dictionary<string, TemperatureThresholds>();

            // Prenumerera på ändringar
            _settingsService.TemperatureSettingsChanged += OnTemperatureSettingsChanged;
        }

        /// <summary>
        /// Hämtar temperaturtröskel för en sensor (med cache)
        /// </summary>
        public async Task<TemperatureThresholds> GetThresholdsAsync(string sensorName)
        {
            lock (_lock)
            {
                if (_cachedThresholds.ContainsKey(sensorName))
                {
                    return _cachedThresholds[sensorName];
                }
            }

            // Ladda från settings om inte i cache
            var thresholds = await LoadThresholdsFromSettings(sensorName);

            lock (_lock)
            {
                _cachedThresholds[sensorName] = thresholds;
            }

            return thresholds;
        }

        /// <summary>
        /// Bestämmer temperaturstatus baserat på värde och trösklar
        /// </summary>
        public async Task<TemperatureStatus> GetTemperatureStatusAsync(string sensorName, double temperature)
        {
            var thresholds = await GetThresholdsAsync(sensorName);

            if (temperature >= thresholds.CriticalTemp)
                return TemperatureStatus.Critical;

            if (temperature >= thresholds.WarningTemp)
                return TemperatureStatus.Warning;

            if (temperature >= thresholds.OptimalMin)
                return TemperatureStatus.Optimal;

            return TemperatureStatus.TooLow;
        }

        /// <summary>
        /// Event handler när temperaturinställningar ändras
        /// </summary>
        private void OnTemperatureSettingsChanged(object sender, TemperatureSettingsChangedEventArgs e)
        {
            lock (_lock)
            {
                // Uppdatera cache med nya värden
                _cachedThresholds[e.SensorName] = new TemperatureThresholds
                {
                    OptimalMin = e.OptimalMin,
                    WarningTemp = e.WarningTemp,
                    CriticalTemp = e.CriticalTemp
                };
            }

            System.Diagnostics.Debug.WriteLine($"Temperaturtröskel uppdaterad för {e.SensorName}: Optimal={e.OptimalMin}, Warning={e.WarningTemp}, Critical={e.CriticalTemp}");
        }

        /// <summary>
        /// Laddar trösklar från settings
        /// </summary>
        private async Task<TemperatureThresholds> LoadThresholdsFromSettings(string sensorName)
        {
            return sensorName switch
            {
                "Kolv" => new TemperatureThresholds
                {
                    OptimalMin = await _settingsService.GetKolvOptimalMinAsync(),
                    WarningTemp = await _settingsService.GetKolvWarningTempAsync(),
                    CriticalTemp = await _settingsService.GetKolvCriticalTempAsync()
                },
                "Destillat" => new TemperatureThresholds
                {
                    OptimalMin = await _settingsService.GetDestillatOptimalMinAsync(),
                    WarningTemp = await _settingsService.GetDestillatWarningTempAsync(),
                    CriticalTemp = await _settingsService.GetDestillatCriticalTempAsync()
                },
                "Kylare" => new TemperatureThresholds
                {
                    OptimalMin = await _settingsService.GetKylareOptimalMinAsync(),
                    WarningTemp = await _settingsService.GetKylareWarningTempAsync(),
                    CriticalTemp = await _settingsService.GetKylareCriticalTempAsync()
                },
                _ => new TemperatureThresholds { OptimalMin = 50, WarningTemp = 80, CriticalTemp = 90 }
            };
        }
    }

    /// <summary>
    /// Temperaturtröskel data
    /// </summary>
    public class TemperatureThresholds
    {
        public double OptimalMin { get; set; }
        public double WarningTemp { get; set; }
        public double CriticalTemp { get; set; }
    }
}
