using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;
using SQLite;

namespace DistilleryMonitor.Mobile.Services
{
    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public DatabaseService()
        {
            // Skapa databas i app's lokala mapp
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "DistilleryMonitor.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_databasePath);

            // Skapa tabell med SQLite attribut
            await _database.CreateTableAsync<TemperatureHistoryEntity>();

            System.Diagnostics.Debug.WriteLine($"📁 Databas skapad: {_databasePath}");
        }

        #region Save Data

        public async Task SaveTemperatureAsync(TemperatureReading reading)
        {
            await InitializeAsync();

            var entity = new TemperatureHistoryEntity
            {
                SensorName = reading.Name,
                SensorId = reading.Id,
                Temperature = reading.Temperature,
                Timestamp = DateTime.Now
            };

            await _database.InsertAsync(entity);
            System.Diagnostics.Debug.WriteLine($"💾 Sparad: {reading.Name} = {reading.Temperature:F1}°C");
        }

        public async Task SaveTemperaturesAsync(List<TemperatureReading> readings)
        {
            await InitializeAsync();

            var entities = readings.Select(r => new TemperatureHistoryEntity
            {
                SensorName = r.Name,
                SensorId = r.Id,
                Temperature = r.Temperature,
                Timestamp = DateTime.Now
            }).ToList();

            await _database.InsertAllAsync(entities);
            System.Diagnostics.Debug.WriteLine($"💾 Sparade {readings.Count} temperaturer");
        }

        #endregion

        #region Get Data

        public async Task<List<TemperatureHistory>> GetHistoryBySensorAsync(string sensorName)
        {
            await InitializeAsync();

            var entities = await _database.Table<TemperatureHistoryEntity>()
                .Where(x => x.SensorName == sensorName)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            return entities.Select(EntityToModel).ToList();
        }

        public async Task<List<TemperatureHistory>> GetHistoryByTimeRangeAsync(DateTime from, DateTime to)
        {
            await InitializeAsync();

            var entities = await _database.Table<TemperatureHistoryEntity>()
                .Where(x => x.Timestamp >= from && x.Timestamp <= to)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            return entities.Select(EntityToModel).ToList();
        }

        public async Task<List<TemperatureHistory>> GetRecentHistoryAsync(int minutes = 60)
        {
            await InitializeAsync();

            var cutoffTime = DateTime.Now.AddMinutes(-minutes);

            var entities = await _database.Table<TemperatureHistoryEntity>()
                .Where(x => x.Timestamp >= cutoffTime)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            return entities.Select(EntityToModel).ToList();
        }

        #endregion

        #region Process Management

        public async Task ClearAllHistoryAsync()
        {
            await InitializeAsync();

            var deletedCount = await _database.DeleteAllAsync<TemperatureHistoryEntity>();
            System.Diagnostics.Debug.WriteLine($"🗑️ Raderade {deletedCount} poster från historik");
        }

        public async Task DeleteOldRecordsAsync(int daysToKeep = 7)
        {
            await InitializeAsync();

            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            var deletedCount = await _database.Table<TemperatureHistoryEntity>()
                .Where(x => x.Timestamp < cutoffDate)
                .DeleteAsync();

            if (deletedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🗑️ Auto-raderade {deletedCount} gamla poster (äldre än {daysToKeep} dagar)");
            }
        }

        public async Task<DateTime?> GetLastDataTimestampAsync()
        {
            await InitializeAsync();

            var lastEntity = await _database.Table<TemperatureHistoryEntity>()
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();

            return lastEntity?.Timestamp;
        }

        public async Task<bool> ShouldPromptForNewProcessAsync(int daysThreshold = 30)
        {
            var lastTimestamp = await GetLastDataTimestampAsync();

            if (lastTimestamp == null)
                return false; // Ingen data = ingen prompt

            var daysSinceLastData = (DateTime.Now - lastTimestamp.Value).TotalDays;
            return daysSinceLastData >= daysThreshold;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Konverterar databas entity till Core model
        /// </summary>
        private TemperatureHistory EntityToModel(TemperatureHistoryEntity entity)
        {
            return new TemperatureHistory
            {
                Id = entity.Id,
                SensorName = entity.SensorName,
                SensorId = entity.SensorId,
                Temperature = entity.Temperature,
                Timestamp = entity.Timestamp
            };
        }

        /// <summary>
        /// Hämta databas statistik (för debugging)
        /// </summary>
        public async Task<int> GetTotalRecordsCountAsync()
        {
            await InitializeAsync();
            return await _database.Table<TemperatureHistoryEntity>().CountAsync();
        }

        #endregion
    }

    #region Database Entity

    /// <summary>
    /// SQLite databas entity - bara för Mobile projektet
    /// </summary>
    [Table("TemperatureHistory")]
    public class TemperatureHistoryEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string SensorName { get; set; } = string.Empty;

        [NotNull]
        public int SensorId { get; set; }

        [NotNull]
        public double Temperature { get; set; }

        [NotNull]
        public DateTime Timestamp { get; set; }
    }

    #endregion
}
