using DistilleryMonitor.Core.Models;

namespace DistilleryMonitor.Core.Services
{
    public interface IDatabaseService
    {
        // Initialization
        Task InitializeAsync();

        // Save data
        Task SaveTemperatureAsync(TemperatureReading reading);          // Manual save
        Task SaveTemperaturesAsync(List<TemperatureReading> readings);  // Background save

        // Get data 
        Task<List<TemperatureHistory>> GetHistoryBySensorAsync(string sensorName);
        Task<List<TemperatureHistory>> GetHistoryByTimeRangeAsync(DateTime from, DateTime to);
        Task<List<TemperatureHistory>> GetRecentHistoryAsync(int minutes = 60);

        // Process management  
        Task ClearAllHistoryAsync();                           // Manual clear
        Task DeleteOldRecordsAsync(int daysToKeep = 7);        // Auto cleanup
        Task<DateTime?> GetLastDataTimestampAsync();           // Kolla senaste data
        Task<bool> ShouldPromptForNewProcessAsync(int daysThreshold = 30); // Auto-prompt logic
    }
}
