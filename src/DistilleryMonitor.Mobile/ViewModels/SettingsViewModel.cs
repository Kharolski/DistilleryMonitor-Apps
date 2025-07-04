﻿using DistilleryMonitor.Core.Services;
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

        #endregion

        #region Methods - Device Discovery

        private async Task SearchForDevicesAsync()
        {
            try
            {
                IsSearching = true;
                FoundDevices.Clear();
                ConnectionTestResult = "";

                DiscoveryStatus = "Söker efter temperaturmätare...";
                bool foundAny = false;

                for (int i = 1; i <= 20; i++)
                {
                    DiscoveryStatus = $"Söker efter temperaturmätare... ({i}/20)";

                    // Testa din kända ESP32 IP
                    if (i == 15) // Halvvägs genom sökningen
                    {
                        bool isReachable = await _apiService.TestConnectionAsync("192.168.39.156");
                        if (isReachable)
                        {
                            FoundDevices.Add(new Esp32Device
                            {
                                Name = "DestillationMonitor",
                                IpAddress = "192.168.39.156",
                                DisplayName = "✅ Temperaturmätare (192.168.39.156)"
                            });
                            foundAny = true;
                        }
                    }

                    // Här kan du lägga till fler IP-adresser att testa:
                    // if (i == 20) { /* testa annan IP */ }

                    await Task.Delay(100); // Snabbare än 1000ms
                }

                // Visa resultat
                if (foundAny)
                {
                    DiscoveryStatus = $"✅ Hittade {FoundDevices.Count} temperaturmätare!";
                }
                else
                {
                    DiscoveryStatus = "❌ Inga temperaturmätare hittades - prova manuell anslutning";

                    // Lägg till hjälptext i dropdown
                    FoundDevices.Add(new Esp32Device
                    {
                        Name = "Ingen hittad",
                        IpAddress = "",
                        DisplayName = "❌ Inga mätare hittades - använd manuell anslutning nedan"
                    });
                }

                await Task.Delay(3000);
                DiscoveryStatus = "";
            }
            catch (Exception ex)
            {
                DiscoveryStatus = $"❌ Fel vid sökning: {ex.Message}";
                await Task.Delay(3000);
                DiscoveryStatus = "";
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
                if (string.IsNullOrEmpty(ManualIpAddress))
                {
                    ConnectionTestResult = "❌ Ange IP-adress först";
                    ConnectionTestColor = "#dc3545";
                    await Task.Delay(3000);
                    ConnectionTestResult = "";
                    return;
                }

                ConnectionTestResult = $"Testar anslutning till {ManualIpAddress}...";
                ConnectionTestColor = "#ffc107"; // Yellow

                bool isSuccess = await _apiService.TestConnectionAsync(ManualIpAddress);

                if (isSuccess)
                {
                    ConnectionTestResult = $"✅ Temperaturmätare svarar på {ManualIpAddress}";
                    ConnectionTestColor = "#28a745"; // Green
                }
                else
                {
                    ConnectionTestResult = $"❌ Ingen temperaturmätare på {ManualIpAddress}";
                    ConnectionTestColor = "#dc3545"; // Red
                }

                await Task.Delay(5000);
                ConnectionTestResult = "";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"❌ Anslutningsfel: {ex.Message}";
                ConnectionTestColor = "#dc3545";
                await Task.Delay(5000);
                ConnectionTestResult = "";
            }
        }

        #endregion

        #region Methods - Settings Management
        /// <summary>
        /// Laddar alla app-inställningar från lagring och uppdaterar UI
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            try
            {
                // Använd alla ISettingsService metoder
                ManualIpAddress = await _settingsService.GetEsp32IpAsync();
                UpdateInterval = await _settingsService.GetUpdateIntervalAsync();
                NotificationsEnabled = await _settingsService.GetNotificationsEnabledAsync();

                // Uppdatera UI på main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(ManualIpAddress));
                    OnPropertyChanged(nameof(UpdateInterval));
                    OnPropertyChanged(nameof(NotificationsEnabled));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Sparar alla app-inställningar och hanterar notifikations-permissions
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            try
            {
                bool wasNotificationsEnabled = await _settingsService.GetNotificationsEnabledAsync();
                bool willEnableNotifications = NotificationsEnabled && !wasNotificationsEnabled;

                // Spara alla inställningar
                await _settingsService.SetEsp32IpAsync(ManualIpAddress);
                await _settingsService.SetPortAsync(80);
                await _settingsService.SetUpdateIntervalAsync((int)UpdateInterval);
                await _settingsService.SetNotificationsEnabledAsync(NotificationsEnabled);
                await _apiService.SetEsp32IpAsync(ManualIpAddress);

                if (willEnableNotifications)
                {
                    ConnectionTestResult = "🔔 Kollar notifikationstillstånd...";
                    ConnectionTestColor = "#ffc107";

                    bool hasSystemPermission = await _notificationService.HasPermissionAsync();

                    if (hasSystemPermission)
                    {
                        // Mobilen har redan permission - aktivera direkt
                        await _notificationService.ShowNotificationAsync(
                            "🎉 Notifikationer aktiverade!",
                            "Du kommer nu få varningar vid kritiska temperaturer.",
                            false);
                        ConnectionTestResult = "✅ Inställningar sparade! Notifikationer aktiverade.";
                        ConnectionTestColor = "#28a745";
                    }
                    else
                    {
                        ConnectionTestResult = "🔔 Begär notifikationstillstånd...";
                        ConnectionTestColor = "#ffc107";

                        bool permissionGranted = await _notificationService.RequestPermissionAsync();

                        if (permissionGranted)
                        {
                            // ✅ Permission godkänd
                            await _notificationService.ShowNotificationAsync(
                                "🎉 Notifikationer aktiverade!",
                                "Du kommer nu få varningar vid kritiska temperaturer.",
                                false);
                            ConnectionTestResult = "✅ Inställningar sparade! Notifikationer aktiverade.";
                            ConnectionTestColor = "#28a745";
                        }
                        else
                        {
                            // ❌ Permission nekad - erbjud direkt hjälp
                            await _settingsService.SetNotificationsEnabledAsync(false);
                            NotificationsEnabled = false;
                            OnPropertyChanged(nameof(NotificationsEnabled));

                            // Erbjud öppna inställningar direkt
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                bool openSettings = await Application.Current.MainPage.DisplayAlert(
                                    "⚠️ Notifikationer blockerade",
                                    "Notifikationer är avstängda i Android-inställningar.\n\n" +
                                    "Detta behövs för temperaturvarningar vid destillation!\n\n" +
                                    "Vill du öppna inställningar för att aktivera dem?",
                                    "Ja, öppna inställningar",
                                    "Nej, senare"
                                );

                                if (openSettings)
                                {
                                    await _notificationService.OpenAppSettingsAsync();

                                    // Instruktion efter öppningen
                                    await Application.Current.MainPage.DisplayAlert(
                                        "📱 Aktivera notifikationer",
                                        "I inställningar som öppnades:\n\n" +
                                        "✅ Aktivera 'Notifikationer' eller 'Notifications'\n" +
                                        "✅ Kom sedan tillbaka hit och försök igen",
                                        "OK"
                                    );
                                }
                            });

                            ConnectionTestResult = "⚠️ Öppna Android-inställningar och aktivera notifikationer.";
                            ConnectionTestColor = "#ffc107";
                        }
                    }
                }
                else
                {
                    ConnectionTestResult = "✅ Inställningar sparade!";
                    ConnectionTestColor = "#28a745";
                }

                await Task.Delay(4000);
                ConnectionTestResult = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SaveSettings error: {ex.Message}");
                ConnectionTestResult = $"❌ Kunde inte spara: {ex.Message}";
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
