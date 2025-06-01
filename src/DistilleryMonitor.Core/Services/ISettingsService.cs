namespace DistilleryMonitor.Core.Services
{
    public interface ISettingsService
    {
        Task<string> GetEsp32IpAsync();
        Task SetEsp32IpAsync(string ipAddress);
        Task<int> GetUpdateIntervalAsync();
        Task SetUpdateIntervalAsync(int seconds);
        Task<bool> GetAutoConnectAsync();
        Task SetAutoConnectAsync(bool autoConnect);
    }
}
