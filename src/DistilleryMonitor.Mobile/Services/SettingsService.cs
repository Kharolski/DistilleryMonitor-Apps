using DistilleryMonitor.Core.Services;

namespace DistilleryMonitor.Mobile.Services
{
    public class SettingsService : ISettingsService
    {
        private const string ESP32_IP_KEY = "esp32_ip";
        private const string UPDATE_INTERVAL_KEY = "update_interval";
        private const string AUTO_CONNECT_KEY = "auto_connect";

        private const string DEFAULT_IP = "192.168.7.75";
        private const int DEFAULT_INTERVAL = 10;
        private const bool DEFAULT_AUTO_CONNECT = true;

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
    }
}
