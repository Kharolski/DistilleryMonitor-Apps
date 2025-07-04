﻿using System.Text;
using System.Text.Json;
using DistilleryMonitor.Core.Models;

namespace DistilleryMonitor.Core.Services;

/// <summary>
/// Service för kommunikation med ESP32 DistilleryMonitor API
/// Hanterar HTTP-anrop för temperaturdata och konfiguration
/// </summary>
public class ApiService
{
    #region Private Fields
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private string _baseUrl = "";
    #endregion

    #region Constructor
    /// <summary>
    /// Konstruktor - initialiserar API service med HTTP-klient och settings
    /// </summary>
    public ApiService(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _baseUrl = "";

        // Ladda sparad IP-adress från settings vid start
        _ = Task.Run(async () =>
        {
            var savedIp = await _settingsService.GetEsp32IpAsync();
            if (!string.IsNullOrEmpty(savedIp))
            {
                _baseUrl = $"http://{savedIp}";
                Console.WriteLine($"🔍 ApiService laddad med IP: {_baseUrl}");
            }
        });
    }
    #endregion

    #region Connection Management
    /// <summary>
    /// Uppdaterar ESP32 IP-adress och sparar i settings
    /// </summary>
    /// <param name="ipAddress">Ny IP-adress (t.ex. "192.168.1.100")</param>
    public async Task SetEsp32IpAsync(string ipAddress)
    {
        await _settingsService.SetEsp32IpAsync(ipAddress);
        _baseUrl = $"http://{ipAddress}";
        Console.WriteLine($"🔍 ApiService uppdaterad till: {_baseUrl}");
    }

    /// <summary>
    /// Hämtar nuvarande ESP32 IP-adress från settings
    /// </summary>
    /// <returns>IP-adress som sträng</returns>
    public async Task<string> GetCurrentIpAsync()
    {
        return await _settingsService.GetEsp32IpAsync();
    }

    /// <summary>
    /// Testar anslutning till ESP32 genom att anropa /api/temperatures
    /// </summary>
    /// <param name="ipAddress">IP att testa (null = använd sparad IP)</param>
    /// <returns>true om anslutning lyckas, false om fel</returns>
    public async Task<bool> TestConnectionAsync(string? ipAddress = null)
    {
        try
        {
            var testIp = ipAddress ?? await _settingsService.GetEsp32IpAsync();
            var testUrl = $"http://{testIp}/api/temperatures";
            Console.WriteLine($"🔍 Testar anslutning till: {testUrl}");

            var response = await _httpClient.GetAsync(testUrl);
            Console.WriteLine($"🔍 HTTP Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Fick svar: {content}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TestConnection fel: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Temperature Data
    /// <summary>
    /// Hämtar aktuella temperaturavläsningar från ESP32
    /// Anropar GET /api/temperatures
    /// </summary>
    /// <returns>TemperatureResponse med alla sensorer, eller null om fel</returns>
    public async Task<TemperatureResponse?> GetTemperaturesAsync()
    {
        try
        {
            var currentIp = await _settingsService.GetEsp32IpAsync();
            if (string.IsNullOrEmpty(currentIp))
            {
                Console.WriteLine("❌ Ingen ESP32 IP-adress inställd");
                return null;
            }

            _baseUrl = $"http://{currentIp}";
            var url = $"{_baseUrl}/api/temperatures";
            Console.WriteLine($"🔍 Hämtar temperaturer från: {url}");

            var response = await _httpClient.GetAsync(url);
            Console.WriteLine($"🔍 HTTP Status: {response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ Fick JSON: {json}");

            return JsonSerializer.Deserialize<TemperatureResponse>(json, GetJsonOptions());
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ HTTP Request Error: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"❌ Timeout Error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ General Error: {ex.Message}");
            Console.WriteLine($"❌ Error Type: {ex.GetType().Name}");
            return null;
        }
    }
    #endregion

    #region Configuration Management
    /// <summary>
    /// Hämtar konfiguration (temperaturintervall) från ESP32
    /// Anropar GET /api/config
    /// </summary>
    /// <returns>ConfigurationResponse med alla sensorinställningar, eller null om fel</returns>
    public async Task<ConfigurationResponse?> GetConfigurationAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                var savedIp = await _settingsService.GetEsp32IpAsync();
                _baseUrl = $"http://{savedIp}";
            }

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/config");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ConfigurationResponse>(json, GetJsonOptions());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting configuration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Skickar ny konfiguration till ESP32
    /// Anropar POST /api/config med JSON-data
    /// </summary>
    /// <param name="config">Ny konfiguration att spara på ESP32</param>
    /// <returns>true om uppdatering lyckades, false om fel</returns>
    public async Task<bool> UpdateConfigurationAsync(ConfigurationResponse config)
    {
        try
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                var savedIp = await _settingsService.GetEsp32IpAsync();
                _baseUrl = $"http://{savedIp}";
            }

            var json = JsonSerializer.Serialize(config, GetJsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/config", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating configuration: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Sensor-Specific Settings (NYA METODER)
    /// <summary>
    /// Hämtar temperaturinställningar för specifik sensor från ESP32
    /// RÄTT mappning: BlueLimit→OptimalMin, GreenLimit→WarningTemp, YellowLimit→CriticalTemp
    /// </summary>
    /// <param name="sensorName">Sensornamn (Kolv, Destillat, Kylare)</param>
    /// <returns>SensorSettings eller null om fel</returns>
    public async Task<SensorSettings?> GetSensorSettingsAsync(string sensorName)
    {
        try
        {
            // Hämta hela konfigurationen från ESP32
            var config = await GetConfigurationAsync();
            if (config == null)
                return null;

            // Hitta rätt sensor baserat på namn
            SensorConfig? sensorConfig = sensorName switch
            {
                "Kolv" => config.Sensor0,
                "Destillat" => config.Sensor1,
                "Kylare" => config.Sensor2,
                _ => null
            };

            if (sensorConfig == null)
                return null;

            // RÄTT konvertering: ESP32 → App
            return new SensorSettings
            {
                SensorName = sensorName,
                OptimalMin = sensorConfig.BlueLimit,      // BlueLimit = när grön börjar
                WarningTemp = sensorConfig.GreenLimit,    // GreenLimit = när gul börjar
                CriticalTemp = sensorConfig.YellowLimit   // YellowLimit = när röd börjar
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting sensor settings for {sensorName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Skickar nya temperaturinställningar för specifik sensor till ESP32
    /// RÄTT mappning: OptimalMin→BlueLimit, WarningTemp→GreenLimit, CriticalTemp→YellowLimit
    /// </summary>
    /// <param name="sensorName">Sensornamn (Kolv, Destillat, Kylare)</param>
    /// <param name="settings">Nya inställningar</param>
    /// <returns>true om uppdatering lyckades</returns>
    public async Task<bool> SetSensorSettingsAsync(string sensorName, SensorSettings settings)
    {
        try
        {
            // Hämta nuvarande konfiguration
            var config = await GetConfigurationAsync();
            if (config == null)
                return false;

            // Uppdatera rätt sensor
            SensorConfig targetSensor = sensorName switch
            {
                "Kolv" => config.Sensor0,
                "Destillat" => config.Sensor1,
                "Kylare" => config.Sensor2,
                _ => throw new ArgumentException($"Okänd sensor: {sensorName}")
            };

            // RÄTT konvertering: App → ESP32
            targetSensor.BlueLimit = settings.OptimalMin;     // OptimalMin → BlueLimit (grön börjar)
            targetSensor.GreenLimit = settings.WarningTemp;   // WarningTemp → GreenLimit (gul börjar)
            targetSensor.YellowLimit = settings.CriticalTemp; // CriticalTemp → YellowLimit (röd börjar)
            targetSensor.Name = sensorName;

            // Skicka hela konfigurationen tillbaka till ESP32
            bool success = await UpdateConfigurationAsync(config);

            if (success)
            {
                Console.WriteLine($"✅ Sensor-inställningar sparade på ESP32 för {sensorName}");
                Console.WriteLine($"   🟢 Grön börjar vid: {settings.OptimalMin}°C → BlueLimit");
                Console.WriteLine($"   🟡 Gul börjar vid: {settings.WarningTemp}°C → GreenLimit");
                Console.WriteLine($"   🔴 Röd börjar vid: {settings.CriticalTemp}°C → YellowLimit");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error setting sensor settings for {sensorName}: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// JSON-serialiseringsinställningar för att matcha ESP32 format
    /// Konverterar C# PascalCase till snake_case (BlueLimit -> blue_limit)
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }
    #endregion
}
