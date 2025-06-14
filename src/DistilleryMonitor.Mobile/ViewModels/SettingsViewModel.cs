using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Mobile.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DistilleryMonitor.Mobile.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly ApiService _apiService;
        private readonly IAppNotificationService _notificationService;

        // Constructor
        public SettingsViewModel(ApiService apiService, ISettingsService settingsService, IAppNotificationService notificationService)
        {
            _apiService = apiService;
            _settingsService = settingsService;
            _notificationService = notificationService;

            // Initialize collections
            FoundDevices = new ObservableCollection<Esp32Device>();

            // Initialize commands
            SearchForDevicesCommand = new Command(async () => await SearchForDevicesAsync());
            TestConnectionCommand = new Command(async () => await TestConnectionAsync());
            SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
            TestNotificationCommand = new Command(async () => await TestNotificationAsync());

            // Load saved settings
            _ = Task.Run(async () => await LoadSettingsAsync());
        }

        #region Properties - ESP32 Connection

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFoundDevices));
            }
        }

        private string _discoveryStatus = "";
        public string DiscoveryStatus
        {
            get => _discoveryStatus;
            set
            {
                _discoveryStatus = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Esp32Device> _foundDevices;
        public ObservableCollection<Esp32Device> FoundDevices
        {
            get => _foundDevices;
            set
            {
                _foundDevices = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFoundDevices));
            }
        }

        public bool HasFoundDevices => FoundDevices?.Count > 0 && !IsSearching;

        private Esp32Device _selectedDevice;
        public Esp32Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();

                // Auto-fill manual IP when device selected
                if (value != null)
                {
                    ManualIpAddress = value.IpAddress;
                }
            }
        }

        private string _manualIpAddress = "";
        public string ManualIpAddress
        {
            get => _manualIpAddress;
            set
            {
                _manualIpAddress = value;
                OnPropertyChanged();
            }
        }

        private string _port = "80";
        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Properties - Connection Test

        private string _connectionTestResult = "";
        public string ConnectionTestResult
        {
            get => _connectionTestResult;
            set
            {
                _connectionTestResult = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowConnectionResult));
            }
        }

        private string _connectionTestColor = "White";
        public string ConnectionTestColor
        {
            get => _connectionTestColor;
            set
            {
                _connectionTestColor = value;
                OnPropertyChanged();
            }
        }

        public bool ShowConnectionResult => !string.IsNullOrEmpty(ConnectionTestResult);

        #endregion

        #region Properties - App Settings

        private bool _useMockData = false; // Default OFF
        public bool UseMockData
        {
            get => _useMockData;
            set
            {
                _useMockData = value;
                OnPropertyChanged();
            }
        }

        private double _updateInterval = 3.0; // Default 3 sekunder
        public double UpdateInterval
        {
            get => _updateInterval;
            set
            {
                _updateInterval = value;
                OnPropertyChanged();
            }
        }

        private bool _notificationsEnabled = true;
        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set
            {
                _notificationsEnabled = value;
                OnPropertyChanged();
            }
        }

        // 5 sekunder för kritisk övervakning
        private int _sensorTimeoutSeconds = 5;
        public int SensorTimeoutSeconds
        {
            get => _sensorTimeoutSeconds;
            set
            {
                _sensorTimeoutSeconds = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands

        public ICommand SearchForDevicesCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand TestNotificationCommand { get; }

        #endregion

        #region Methods - Device Discovery

        private async Task SearchForDevicesAsync()
        {
            try
            {
                IsSearching = true;
                FoundDevices.Clear();
                ConnectionTestResult = "";

                // Simulate discovery process with countdown
                for (int i = 30; i > 0; i--)
                {
                    DiscoveryStatus = $"Söker efter ESP32 enheter... ({i}/30 sek kvar)";

                    if (i == 25) // Din riktiga ESP32
                    {
                        FoundDevices.Add(new Esp32Device
                        {
                            Name = "DestillationMonitor",
                            IpAddress = "192.168.39.156", // ← Din riktiga IP
                            DisplayName = "DestillationMonitor (192.168.39.156) - RIKTIG ESP32"
                        });
                    }

                    if (i == 20) // Mock device för testning
                    {
                        FoundDevices.Add(new Esp32Device
                        {
                            Name = "DestillationMonitor",
                            IpAddress = "192.168.39.200", // ← Annan mock IP
                            DisplayName = "DestillationMonitor (192.168.39.200) - MOCK"
                        });
                    }

                    await Task.Delay(1000);
                }

                DiscoveryStatus = $"Sökning klar! Hittade {FoundDevices.Count} enheter.";
                await Task.Delay(3000);
                DiscoveryStatus = "";
            }
            catch (Exception ex)
            {
                DiscoveryStatus = $"Fel vid sökning: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        #endregion

        #region Methods - Connection Test

        private async Task TestConnectionAsync()
        {
            try
            {
                ConnectionTestResult = "Testar anslutning...";
                ConnectionTestColor = "#ffc107"; // Yellow

                string ipToTest = !string.IsNullOrEmpty(ManualIpAddress) ? ManualIpAddress : "192.168.1.100";

                // Använd din befintliga ApiService
                bool isSuccess = await _apiService.TestConnectionAsync(ipToTest);

                if (isSuccess)
                {
                    ConnectionTestResult = $"✅ Anslutning lyckades till {ipToTest}:{Port}";
                    ConnectionTestColor = "#28a745"; // Green
                }
                else
                {
                    ConnectionTestResult = $"❌ Kunde inte ansluta till {ipToTest}:{Port}";
                    ConnectionTestColor = "#dc3545"; // Red
                }

                // Clear result after 5 seconds
                await Task.Delay(5000);
                ConnectionTestResult = "";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"❌ Fel: {ex.Message}";
                ConnectionTestColor = "#dc3545";
            }
        }

        #endregion

        #region Methods - Settings Management

        private async Task LoadSettingsAsync()
        {
            try
            {
                // Använd alla ISettingsService metoder
                ManualIpAddress = await _settingsService.GetEsp32IpAsync();
                Port = (await _settingsService.GetPortAsync()).ToString();
                UseMockData = await _settingsService.GetUseMockDataAsync();
                UpdateInterval = await _settingsService.GetUpdateIntervalAsync();
                NotificationsEnabled = await _settingsService.GetNotificationsEnabledAsync();

                // Uppdatera UI på main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(ManualIpAddress));
                    OnPropertyChanged(nameof(Port));
                    OnPropertyChanged(nameof(UseMockData));
                    OnPropertyChanged(nameof(UpdateInterval));
                    OnPropertyChanged(nameof(NotificationsEnabled));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                // Spara alla inställningar
                await _settingsService.SetEsp32IpAsync(ManualIpAddress);
                await _settingsService.SetPortAsync(int.TryParse(Port, out int p) ? p : 80);
                await _settingsService.SetUseMockDataAsync(UseMockData);
                await _settingsService.SetUpdateIntervalAsync((int)UpdateInterval);
                await _settingsService.SetNotificationsEnabledAsync(NotificationsEnabled);

                // Uppdatera ApiService med ny IP
                await _apiService.SetEsp32IpAsync(ManualIpAddress);

                // Show success message
                ConnectionTestResult = "✅ Inställningar sparade!";
                ConnectionTestColor = "#28a745";

                await Task.Delay(3000);
                ConnectionTestResult = "";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"❌ Kunde inte spara: {ex.Message}";
                ConnectionTestColor = "#dc3545";
            }
        }

        private async Task TestNotificationAsync()
        {
            try
            {
                // Först be om tillstånd
                bool hasPermission = await _notificationService.RequestPermissionAsync();

                if (!hasPermission)
                {
                    ConnectionTestResult = "❌ Notifikationstillstånd nekades";
                    ConnectionTestColor = "#dc3545";
                    return;
                }

                // Skicka test-notifikation
                await _notificationService.ShowNotificationAsync(
                    "🧪 Test Notifikation",
                    "Notifikationer fungerar perfekt!",
                    false);

                // Visa bekräftelse
                ConnectionTestResult = "✅ Test-notifikation skickad!";
                ConnectionTestColor = "#28a745";

                // Rensa efter 3 sekunder
                await Task.Delay(3000);
                ConnectionTestResult = "";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"❌ Notifikationsfel: {ex.Message}";
                ConnectionTestColor = "#dc3545";
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

    #region Helper Classes

    public class Esp32Device
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string DisplayName { get; set; }
    }

    #endregion
}
