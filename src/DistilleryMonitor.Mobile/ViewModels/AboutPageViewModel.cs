using DistilleryMonitor.Core.Services;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DistilleryMonitor.Mobile.ViewModels
{
    public class AboutPageViewModel : INotifyPropertyChanged
    {
        private readonly IDeveloperService _developerService;
        private CancellationTokenSource _progressCancellation;

        public AboutPageViewModel(IDeveloperService developerService)
        {
            _developerService = developerService;

            // Initialize collections
            DebugLogs = new ObservableCollection<string>();

            // Initialize commands
            ToggleMockDataCommand = new Command(async () => await ToggleMockDataAsync());
            RefreshLogsCommand = new Command(async () => await RefreshLogsAsync());
            ClearLogsCommand = new Command(async () => await ClearLogsAsync());

            VersionTappedCommand = new Command(async () => await OnVersionTappedAsync());

            // Load initial data
            _ = Task.Run(async () => await LoadDataAsync());
        }

        #region Properties - Developer Mode
        private int _versionTapCount = 0;

        private string _progressMessage = "";
        public string ProgressMessage
        {
            get => _progressMessage;
            set
            {
                _progressMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowProgressMessage));
            }
        }

        public bool ShowProgressMessage => !string.IsNullOrEmpty(ProgressMessage);
        private bool _showDeveloperSection = false;
        public bool ShowDeveloperSection
        {
            get => _showDeveloperSection;
            set
            {
                _showDeveloperSection = value;
                OnPropertyChanged();
            }
        }

        private bool _isDeveloperMode;
        public bool IsDeveloperMode
        {
            get => _isDeveloperMode;
            set
            {
                _isDeveloperMode = value;
                OnPropertyChanged();

                // Dölj hela sektionen när toggle stängs av
                if (!value)
                {
                    ShowDeveloperSection = false;
                }

                // Kör toggle-logik direkt
                _ = Task.Run(async () => await HandleDeveloperModeChangedAsync(value));
            }
        }

        private async Task HandleDeveloperModeChangedAsync(bool newValue)
        {
            try
            {
                await _developerService.SetDeveloperModeAsync(newValue);
                await RefreshLogsAsync();
            }
            catch (Exception ex)
            {
                await _developerService.AddDebugLogAsync($"Fel vid toggle Developer Mode: {ex.Message}");
            }
        }

        private bool _useMockData;
        public bool UseMockData
        {
            get => _useMockData;
            set
            {
                _useMockData = value;
                OnPropertyChanged();

                // Lägg till logik som IsDeveloperMode har:
                _ = Task.Run(async () => await HandleMockDataChangedAsync(value));
            }
        }

        private async Task HandleMockDataChangedAsync(bool newValue)
        {
            try
            {
                await _developerService.SetUseMockDataAsync(newValue);
                await _developerService.AddDebugLogAsync($"Mock Data {(newValue ? "aktiverat" : "inaktiverat")}");
                await RefreshLogsAsync();
            }
            catch (Exception ex)
            {
                await _developerService.AddDebugLogAsync($"Fel vid toggle Mock Data: {ex.Message}");
            }
        }
        #endregion

        #region Properties - Debug Logs
        private ObservableCollection<string> _debugLogs;
        public ObservableCollection<string> DebugLogs
        {
            get => _debugLogs;
            set
            {
                _debugLogs = value;
                OnPropertyChanged();
            }
        }

        private int _logCountToShow = 20;
        public int LogCountToShow
        {
            get => _logCountToShow;
            set
            {
                if (value >= 1 && value <= 100)
                {
                    _logCountToShow = value;
                    OnPropertyChanged();
                    // Auto-refresh när antal ändras
                    _ = Task.Run(async () => await RefreshLogsAsync());
                }
            }
        }

        private int _totalLogCount;
        public int TotalLogCount
        {
            get => _totalLogCount;
            set
            {
                _totalLogCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LogCountInfo));
            }
        }

        public string LogCountInfo => $"Visar {Math.Min(LogCountToShow, TotalLogCount)} av {TotalLogCount} loggar";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand ToggleDeveloperModeCommand { get; }
        public ICommand ToggleMockDataCommand { get; }
        public ICommand RefreshLogsCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand VersionTappedCommand { get; }
        #endregion

        #region Methods - Data Loading
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Load developer settings
                IsDeveloperMode = await _developerService.GetDeveloperModeAsync();
                UseMockData = await _developerService.GetUseMockDataAsync();

                // Load debug logs
                await RefreshLogsAsync();

                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(IsDeveloperMode));
                    OnPropertyChanged(nameof(UseMockData));
                });
            }
            catch (Exception ex)
            {
                await _developerService.AddDebugLogAsync($"Fel vid laddning av AboutPage: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Methods - Developer Mode
        private async Task OnVersionTappedAsync()
        {
            _versionTapCount++;

            // Avbryt tidigare timer om den körs
            _progressCancellation?.Cancel();
            _progressCancellation = new CancellationTokenSource();

            if (_versionTapCount >= 7 && !ShowDeveloperSection)
            {
                ShowDeveloperSection = true;
                IsDeveloperMode = true;

                await _developerService.SetDeveloperModeAsync(true);
                await _developerService.AddDebugLogAsync("🔓 Developer Mode upplåst!");
                await RefreshLogsAsync();

                ProgressMessage = "🔓 Developer Mode aktiverat!";

                // Längre delay för framgång
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(4000, _progressCancellation.Token);
                        ProgressMessage = "";
                    }
                    catch (OperationCanceledException) { }
                });

                _versionTapCount = 0;
            }
            else if (_versionTapCount >= 3 && _versionTapCount < 7)
            {
                int remaining = 7 - _versionTapCount;
                string message = remaining == 1 ? "🔓 Ett steg kvar..." : $"🔓 {remaining} steg kvar...";

                ProgressMessage = message;

                // Kortare delay för progress
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000, _progressCancellation.Token);
                        ProgressMessage = "";
                    }
                    catch (OperationCanceledException) { }
                });
            }
        }

        private async Task ToggleMockDataAsync()
        {
            try
            {
                var newValue = !UseMockData;
                await _developerService.SetUseMockDataAsync(newValue);
                UseMockData = newValue;

                await _developerService.AddDebugLogAsync($"Mock Data {(newValue ? "aktiverat" : "inaktiverat")}");

                // Refresh logs to show the new entry
                await RefreshLogsAsync();
            }
            catch (Exception ex)
            {
                await _developerService.AddDebugLogAsync($"Fel vid toggle Mock Data: {ex.Message}");
            }
        }
        #endregion

        #region Methods - Debug Logs
        private async Task RefreshLogsAsync()
        {
            try
            {
                var logs = await _developerService.GetDebugLogsAsync(LogCountToShow);
                TotalLogCount = await _developerService.GetDebugLogCountAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DebugLogs.Clear();
                    foreach (var log in logs)
                    {
                        DebugLogs.Add(log);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid refresh av loggar: {ex.Message}");
            }
        }

        private async Task ClearLogsAsync()
        {
            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Rensa loggar",
                    "Är du säker på att du vill rensa alla debug-loggar?",
                    "Ja, rensa",
                    "Avbryt"
                );

                if (confirm)
                {
                    await _developerService.ClearDebugLogsAsync();
                    await _developerService.AddDebugLogAsync("Debug-loggar rensade av användare");
                    await RefreshLogsAsync();
                }
            }
            catch (Exception ex)
            {
                await _developerService.AddDebugLogAsync($"Fel vid rensning av loggar: {ex.Message}");
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
