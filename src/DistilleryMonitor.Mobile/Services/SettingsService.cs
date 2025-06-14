using DistilleryMonitor.Core.Services;

namespace DistilleryMonitor.Mobile.Services
{
    public class SettingsService : ISettingsService
    {
        // Befintliga konstanter
        private const string ESP32_IP_KEY = "esp32_ip";
        private const string UPDATE_INTERVAL_KEY = "update_interval";
        private const string AUTO_CONNECT_KEY = "auto_connect";

        // Nya konstanter för Settings UI
        private const string USE_MOCK_DATA_KEY = "use_mock_data";
        private const string NOTIFICATIONS_ENABLED_KEY = "notifications_enabled";
        private const string ESP32_PORT_KEY = "esp32_port";

        // Temperaturinställningar konstanter
        // Kolv
        private const string KOLV_OPTIMAL_MIN_KEY = "kolv_optimal_min";
        private const string KOLV_WARNING_TEMP_KEY = "kolv_warning_temp";
        private const string KOLV_CRITICAL_TEMP_KEY = "kolv_critical_temp";

        // Destillat
        private const string DESTILLAT_OPTIMAL_MIN_KEY = "destillat_optimal_min";
        private const string DESTILLAT_WARNING_TEMP_KEY = "destillat_warning_temp";
        private const string DESTILLAT_CRITICAL_TEMP_KEY = "destillat_critical_temp";

        // Kylare
        private const string KYLARE_OPTIMAL_MIN_KEY = "kylare_optimal_min";
        private const string KYLARE_WARNING_TEMP_KEY = "kylare_warning_temp";
        private const string KYLARE_CRITICAL_TEMP_KEY = "kylare_critical_temp";

        // Befintliga defaults
        private const string DEFAULT_IP = "192.168.7.75";
        private const int DEFAULT_INTERVAL = 3;
        private const bool DEFAULT_AUTO_CONNECT = true;

        // Nya defaults
        private const bool DEFAULT_USE_MOCK_DATA = false;
        private const bool DEFAULT_NOTIFICATIONS_ENABLED = true;
        private const int DEFAULT_PORT = 80;

        // Temperatur defaults (samma som dina hårdkodade värden)
        // Kolv defaults
        private const double DEFAULT_KOLV_OPTIMAL_MIN = 70.0;
        private const double DEFAULT_KOLV_WARNING_TEMP = 80.0;
        private const double DEFAULT_KOLV_CRITICAL_TEMP = 90.0;

        // Destillat defaults
        private const double DEFAULT_DESTILLAT_OPTIMAL_MIN = 75.0;
        private const double DEFAULT_DESTILLAT_WARNING_TEMP = 85.0;
        private const double DEFAULT_DESTILLAT_CRITICAL_TEMP = 95.0;

        // Kylare defaults
        private const double DEFAULT_KYLARE_OPTIMAL_MIN = 20.0;
        private const double DEFAULT_KYLARE_WARNING_TEMP = 30.0;
        private const double DEFAULT_KYLARE_CRITICAL_TEMP = 40.0;

        #region Befintliga metoder
        public async Task<string> GetEsp32IpAsync()
        {
            return await SecureStorage.GetAsync(ESP32_IP_KEY) ?? DEFAULT_IP;
        }

        public async Task SetEsp32IpAsync(string ipAddress)
        {
            await SecureStorage.SetAsync(ESP32_IP_KEY, ipAddress);
        }

        public async Task<int> GetUpdateIntervalAsync()
        {
            var stored = await SecureStorage.GetAsync(UPDATE_INTERVAL_KEY);
            return int.TryParse(stored, out var interval) ? interval : DEFAULT_INTERVAL;
        }

        public async Task SetUpdateIntervalAsync(int seconds)
        {
            await SecureStorage.SetAsync(UPDATE_INTERVAL_KEY, seconds.ToString());
        }

        public async Task<bool> GetAutoConnectAsync()
        {
            var stored = await SecureStorage.GetAsync(AUTO_CONNECT_KEY);
            return bool.TryParse(stored, out var autoConnect) ? autoConnect : DEFAULT_AUTO_CONNECT;
        }

        public async Task SetAutoConnectAsync(bool autoConnect)
        {
            await SecureStorage.SetAsync(AUTO_CONNECT_KEY, autoConnect.ToString());
        }
        #endregion

        #region Nya metoder för Settings UI
        public async Task<bool> GetUseMockDataAsync()
        {
            var stored = await SecureStorage.GetAsync(USE_MOCK_DATA_KEY);
            return bool.TryParse(stored, out var useMockData) ? useMockData : DEFAULT_USE_MOCK_DATA;
        }

        public async Task SetUseMockDataAsync(bool useMockData)
        {
            await SecureStorage.SetAsync(USE_MOCK_DATA_KEY, useMockData.ToString());
        }

        public async Task<bool> GetNotificationsEnabledAsync()
        {
            var stored = await SecureStorage.GetAsync(NOTIFICATIONS_ENABLED_KEY);
            return bool.TryParse(stored, out var enabled) ? enabled : DEFAULT_NOTIFICATIONS_ENABLED;
        }

        public async Task SetNotificationsEnabledAsync(bool enabled)
        {
            await SecureStorage.SetAsync(NOTIFICATIONS_ENABLED_KEY, enabled.ToString());
        }

        public async Task<int> GetPortAsync()
        {
            var stored = await SecureStorage.GetAsync(ESP32_PORT_KEY);
            return int.TryParse(stored, out var port) ? port : DEFAULT_PORT;
        }

        public async Task SetPortAsync(int port)
        {
            await SecureStorage.SetAsync(ESP32_PORT_KEY, port.ToString());
        }
        #endregion

        #region Temperaturinställningar

        // Kolv temperaturinställningar
        public async Task<double> GetKolvOptimalMinAsync()
        {
            var stored = await SecureStorage.GetAsync(KOLV_OPTIMAL_MIN_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KOLV_OPTIMAL_MIN;
        }

        public async Task SetKolvOptimalMinAsync(double temperature)
        {
            await SecureStorage.SetAsync(KOLV_OPTIMAL_MIN_KEY, temperature.ToString());
        }

        public async Task<double> GetKolvWarningTempAsync()
        {
            var stored = await SecureStorage.GetAsync(KOLV_WARNING_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KOLV_WARNING_TEMP;
        }

        public async Task SetKolvWarningTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(KOLV_WARNING_TEMP_KEY, temperature.ToString());
        }

        public async Task<double> GetKolvCriticalTempAsync()
        {
            var stored = await SecureStorage.GetAsync(KOLV_CRITICAL_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KOLV_CRITICAL_TEMP;
        }

        public async Task SetKolvCriticalTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(KOLV_CRITICAL_TEMP_KEY, temperature.ToString());
        }

        // Destillat temperaturinställningar
        public async Task<double> GetDestillatOptimalMinAsync()
        {
            var stored = await SecureStorage.GetAsync(DESTILLAT_OPTIMAL_MIN_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_DESTILLAT_OPTIMAL_MIN;
        }

        public async Task SetDestillatOptimalMinAsync(double temperature)
        {
            await SecureStorage.SetAsync(DESTILLAT_OPTIMAL_MIN_KEY, temperature.ToString());
        }

        public async Task<double> GetDestillatWarningTempAsync()
        {
            var stored = await SecureStorage.GetAsync(DESTILLAT_WARNING_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_DESTILLAT_WARNING_TEMP;
        }

        public async Task SetDestillatWarningTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(DESTILLAT_WARNING_TEMP_KEY, temperature.ToString());
        }

        public async Task<double> GetDestillatCriticalTempAsync()
        {
            var stored = await SecureStorage.GetAsync(DESTILLAT_CRITICAL_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_DESTILLAT_CRITICAL_TEMP;
        }

        public async Task SetDestillatCriticalTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(DESTILLAT_CRITICAL_TEMP_KEY, temperature.ToString());
        }

        // Kylare temperaturinställningar
        public async Task<double> GetKylareOptimalMinAsync()
        {
            var stored = await SecureStorage.GetAsync(KYLARE_OPTIMAL_MIN_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KYLARE_OPTIMAL_MIN;
        }

        public async Task SetKylareOptimalMinAsync(double temperature)
        {
            await SecureStorage.SetAsync(KYLARE_OPTIMAL_MIN_KEY, temperature.ToString());
        }

        public async Task<double> GetKylareWarningTempAsync()
        {
            var stored = await SecureStorage.GetAsync(KYLARE_WARNING_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KYLARE_WARNING_TEMP;
        }

        public async Task SetKylareWarningTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(KYLARE_WARNING_TEMP_KEY, temperature.ToString());
        }

        public async Task<double> GetKylareCriticalTempAsync()
        {
            var stored = await SecureStorage.GetAsync(KYLARE_CRITICAL_TEMP_KEY);
            return double.TryParse(stored, out var temp) ? temp : DEFAULT_KYLARE_CRITICAL_TEMP;
        }

        public async Task SetKylareCriticalTempAsync(double temperature)
        {
            await SecureStorage.SetAsync(KYLARE_CRITICAL_TEMP_KEY, temperature.ToString());
        }

        #endregion

        #region Utility metoder
        /// <summary>
        /// Återställer alla inställningar till standardvärden
        /// </summary>
        public async Task ResetToDefaultsAsync()
        {
            // Befintliga
            await SecureStorage.SetAsync(ESP32_IP_KEY, DEFAULT_IP);
            await SecureStorage.SetAsync(UPDATE_INTERVAL_KEY, DEFAULT_INTERVAL.ToString());
            await SecureStorage.SetAsync(AUTO_CONNECT_KEY, DEFAULT_AUTO_CONNECT.ToString());
            await SecureStorage.SetAsync(USE_MOCK_DATA_KEY, DEFAULT_USE_MOCK_DATA.ToString());
            await SecureStorage.SetAsync(NOTIFICATIONS_ENABLED_KEY, DEFAULT_NOTIFICATIONS_ENABLED.ToString());
            await SecureStorage.SetAsync(ESP32_PORT_KEY, DEFAULT_PORT.ToString());

            // Temperaturinställningar
            // Kolv
            await SecureStorage.SetAsync(KOLV_OPTIMAL_MIN_KEY, DEFAULT_KOLV_OPTIMAL_MIN.ToString());
            await SecureStorage.SetAsync(KOLV_WARNING_TEMP_KEY, DEFAULT_KOLV_WARNING_TEMP.ToString());
            await SecureStorage.SetAsync(KOLV_CRITICAL_TEMP_KEY, DEFAULT_KOLV_CRITICAL_TEMP.ToString());

            // Destillat
            await SecureStorage.SetAsync(DESTILLAT_OPTIMAL_MIN_KEY, DEFAULT_DESTILLAT_OPTIMAL_MIN.ToString());
            await SecureStorage.SetAsync(DESTILLAT_WARNING_TEMP_KEY, DEFAULT_DESTILLAT_WARNING_TEMP.ToString());
            await SecureStorage.SetAsync(DESTILLAT_CRITICAL_TEMP_KEY, DEFAULT_DESTILLAT_CRITICAL_TEMP.ToString());

            // Kylare
            await SecureStorage.SetAsync(KYLARE_OPTIMAL_MIN_KEY, DEFAULT_KYLARE_OPTIMAL_MIN.ToString());
            await SecureStorage.SetAsync(KYLARE_WARNING_TEMP_KEY, DEFAULT_KYLARE_WARNING_TEMP.ToString());
            await SecureStorage.SetAsync(KYLARE_CRITICAL_TEMP_KEY, DEFAULT_KYLARE_CRITICAL_TEMP.ToString());
        }

        /// <summary>
        /// Kontrollerar om det är första gången appen körs
        /// </summary>
        public async Task<bool> IsFirstRunAsync()
        {
            var hasIp = await SecureStorage.GetAsync(ESP32_IP_KEY);
            return string.IsNullOrEmpty(hasIp);
        }

        /// <summary>
        /// Hämtar fullständig ESP32 URL (http://ip:port)
        /// </summary>
        public async Task<string> GetEsp32BaseUrlAsync()
        {
            var ip = await GetEsp32IpAsync();
            var port = await GetPortAsync();
            return $"http://{ip}:{port}";
        }
        #endregion
    }
}
