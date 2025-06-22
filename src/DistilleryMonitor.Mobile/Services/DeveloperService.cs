using DistilleryMonitor.Core.Services;
using SQLite;

namespace DistilleryMonitor.Mobile.Services
{
    public class DeveloperService : IDeveloperService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        // Keys för SecureStorage (Developer Mode + Mock Data)
        private const string DEVELOPER_MODE_KEY = "developer_mode";
        private const string MOCK_DATA_KEY = "use_mock_data";
        private const int MAX_LOGS = 100;

        public DeveloperService()
        {
            // Samma databas som DatabaseService
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "DistilleryMonitor.db3");
        }

        private async Task InitializeDatabaseAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_databasePath);
            await _database.CreateTableAsync<DebugLogEntity>();
        }

        #region Developer Mode (SecureStorage)
        public async Task<bool> GetDeveloperModeAsync()
        {
            var stored = await SecureStorage.GetAsync(DEVELOPER_MODE_KEY);
            return bool.TryParse(stored, out var enabled) ? enabled : false;
        }

        public async Task SetDeveloperModeAsync(bool enabled)
        {
            await SecureStorage.SetAsync(DEVELOPER_MODE_KEY, enabled.ToString());
        }
        #endregion

        #region Mock Data (SecureStorage)
        public async Task<bool> GetUseMockDataAsync()
        {
            var stored = await SecureStorage.GetAsync(MOCK_DATA_KEY);
            return bool.TryParse(stored, out var useMockData) ? useMockData : false;
        }

        public async Task SetUseMockDataAsync(bool useMock)
        {
            await SecureStorage.SetAsync(MOCK_DATA_KEY, useMock.ToString());
        }
        #endregion

        #region Debug Logging (SQLite)
        public async Task AddDebugLogAsync(string message)
        {
            try
            {
                await InitializeDatabaseAsync();

                var logEntry = new DebugLogEntity
                {
                    Message = message,
                    Timestamp = DateTime.Now
                };

                await _database.InsertAsync(logEntry);

                // Rensa gamla loggar (behåll senaste MAX_LOGS)
                await CleanupOldLogsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid sparande av debug-logg: {ex.Message}");
            }
        }

        public async Task<List<string>> GetDebugLogsAsync(int count)
        {
            try
            {
                await InitializeDatabaseAsync();

                var logs = await _database.Table<DebugLogEntity>()
                    .OrderByDescending(x => x.Timestamp)
                    .Take(count)
                    .ToListAsync();

                return logs.Select(log => $"{log.Timestamp:yyyy-MM-dd HH:mm:ss} - {log.Message}").ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<string>> GetAllDebugLogsAsync()
        {
            try
            {
                await InitializeDatabaseAsync();

                var logs = await _database.Table<DebugLogEntity>()
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();

                return logs.Select(log => $"{log.Timestamp:yyyy-MM-dd HH:mm:ss} - {log.Message}").ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task ClearDebugLogsAsync()
        {
            try
            {
                await InitializeDatabaseAsync();
                await _database.DeleteAllAsync<DebugLogEntity>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid rensning av debug-loggar: {ex.Message}");
            }
        }

        public async Task<int> GetDebugLogCountAsync()
        {
            try
            {
                await InitializeDatabaseAsync();
                return await _database.Table<DebugLogEntity>().CountAsync();
            }
            catch
            {
                return 0;
            }
        }

        private async Task CleanupOldLogsAsync()
        {
            try
            {
                var totalCount = await _database.Table<DebugLogEntity>().CountAsync();

                if (totalCount > MAX_LOGS)
                {
                    // Ta bort äldsta loggar, behåll senaste MAX_LOGS
                    var logsToDelete = await _database.Table<DebugLogEntity>()
                        .OrderBy(x => x.Timestamp)
                        .Take(totalCount - MAX_LOGS)
                        .ToListAsync();

                    foreach (var log in logsToDelete)
                    {
                        await _database.DeleteAsync(log);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid rensning av gamla loggar: {ex.Message}");
            }
        }
        #endregion
    }

    #region Database Entity
    [Table("DebugLogs")]
    public class DebugLogEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Message { get; set; } = string.Empty;

        [NotNull]
        public DateTime Timestamp { get; set; }
    }
    #endregion
}
