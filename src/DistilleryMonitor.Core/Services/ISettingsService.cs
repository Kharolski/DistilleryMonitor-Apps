namespace DistilleryMonitor.Core.Services
{
    public interface ISettingsService
    {
        // Befintliga metoder
        Task<string> GetEsp32IpAsync();
        Task SetEsp32IpAsync(string ipAddress);
        Task<int> GetUpdateIntervalAsync();
        Task SetUpdateIntervalAsync(int seconds);
        Task<bool> GetAutoConnectAsync();
        Task SetAutoConnectAsync(bool autoConnect);

        // Nya metoder för Settings UI
        Task<bool> GetUseMockDataAsync();
        Task SetUseMockDataAsync(bool useMockData);
        Task<bool> GetNotificationsEnabledAsync();
        Task SetNotificationsEnabledAsync(bool enabled);
        Task<int> GetPortAsync();
        Task SetPortAsync(int port);

        // Temperaturinställningar för Kolv
        Task<double> GetKolvOptimalMinAsync();
        Task SetKolvOptimalMinAsync(double temperature);
        Task<double> GetKolvWarningTempAsync();
        Task SetKolvWarningTempAsync(double temperature);
        Task<double> GetKolvCriticalTempAsync();
        Task SetKolvCriticalTempAsync(double temperature);

        // Temperaturinställningar för Destillat
        Task<double> GetDestillatOptimalMinAsync();
        Task SetDestillatOptimalMinAsync(double temperature);
        Task<double> GetDestillatWarningTempAsync();
        Task SetDestillatWarningTempAsync(double temperature);
        Task<double> GetDestillatCriticalTempAsync();
        Task SetDestillatCriticalTempAsync(double temperature);

        // Temperaturinställningar för Kylare
        Task<double> GetKylareOptimalMinAsync();
        Task SetKylareOptimalMinAsync(double temperature);
        Task<double> GetKylareWarningTempAsync();
        Task SetKylareWarningTempAsync(double temperature);
        Task<double> GetKylareCriticalTempAsync();
        Task SetKylareCriticalTempAsync(double temperature);

        // Bonus utility metoder
        Task ResetToDefaultsAsync();
        Task<bool> IsFirstRunAsync();
        Task<string> GetEsp32BaseUrlAsync();
    }
}
