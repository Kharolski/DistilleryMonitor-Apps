using System.Text;
using System.Text.Json;
using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Core.Services;

/// <summary>
/// Service för kommunikation med ESP32 DistilleryMonitor API
/// Hanterar HTTP-anrop för temperaturdata och konfiguration
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private string _baseUrl;

    /// <summary>
    /// Konstruktor - initialiserar API service med HTTP-klient och settings
    /// </summary>
    public ApiService(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _baseUrl = "http://192.168.7.75"; // Fallback IP om settings inte finns

        // Ladda sparad IP-adress från settings vid start (asynkront)
        _ = Task.Run(async () =>
        {
            var savedIp = await _settingsService.GetEsp32IpAsync();
            _baseUrl = $"http://{savedIp}";
        });
    }

    /// <summary>
    /// Uppdaterar ESP32 IP-adress och sparar i settings
    /// </summary>
    /// <param name="ipAddress">Ny IP-adress (t.ex. "192.168.1.100")</param>
    public async Task SetEsp32IpAsync(string ipAddress)
    {
        await _settingsService.SetEsp32IpAsync(ipAddress);
        _baseUrl = $"http://{ipAddress}";
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

            var response = await _httpClient.GetAsync(testUrl);
            return response.IsSuccessStatusCode; // 200 OK = anslutning fungerar
        }
        catch
        {
            return false; // Alla fel = anslutning fungerar inte
        }
    }

    /// <summary>
    /// Hämtar aktuella temperaturavläsningar från ESP32
    /// Anropar GET /api/temperatures
    /// </summary>
    /// <returns>TemperatureResponse med alla sensorer, eller null om fel</returns>
    public async Task<TemperatureResponse?> GetTemperaturesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/temperatures");
            response.EnsureSuccessStatusCode(); // Kasta exception om HTTP-fel

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TemperatureResponse>(json, GetJsonOptions());
        }
        catch (Exception ex)
        {
            // Logga fel för debugging
            Console.WriteLine($"Error getting temperatures: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Hämtar konfiguration (temperaturintervall) från ESP32
    /// Anropar GET /api/config
    /// </summary>
    /// <returns>ConfigurationResponse med alla sensorinställningar, eller null om fel</returns>
    public async Task<ConfigurationResponse?> GetConfigurationAsync()
    {
        try
        {
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
            // Konvertera C# objekt till JSON
            var json = JsonSerializer.Serialize(config, GetJsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/config", content);
            return response.IsSuccessStatusCode; // 200 OK = uppdatering lyckades
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating configuration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// JSON-serialiseringsinställningar för att matcha ESP32 format
    /// Konverterar C# PascalCase till snake_case (BlueLimit -> blue_limit)
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // C# -> JSON naming
            PropertyNameCaseInsensitive = true // Tillåt olika case vid läsning
        };
    }
}
