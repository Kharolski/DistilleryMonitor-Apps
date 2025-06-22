namespace DistilleryMonitor.Core.Services
{
    public interface IDeveloperService
    {
        // Developer Mode - permanent lagring
        Task<bool> GetDeveloperModeAsync();
        Task SetDeveloperModeAsync(bool enabled);

        // Mock Data
        Task<bool> GetUseMockDataAsync();
        Task SetUseMockDataAsync(bool useMock);

        // Debug Logging - flexibel antal
        Task AddDebugLogAsync(string message);
        Task<List<string>> GetDebugLogsAsync(int count); // ← Ingen default, utvecklaren väljer
        Task<List<string>> GetAllDebugLogsAsync(); // Alla (max 100)
        Task ClearDebugLogsAsync();
        Task<int> GetDebugLogCountAsync();
    }
}
