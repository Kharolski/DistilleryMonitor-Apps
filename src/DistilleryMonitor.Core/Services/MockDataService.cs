using DistilleryMonitor.Core.Models;
using DistilleryMonitor.Core.Services;

namespace DistilleryMonitor.Core.Services;

/// <summary>
/// Mock data service för utveckling och testning
/// Simulerar ESP32 temperaturdata med realistiska värden och Settings-integration
/// </summary>
public class MockDataService
{
    private readonly Random _random = new();
    private readonly ISettingsService? _settingsService;

    // Håll koll på senaste temperaturer för realistisk variation
    private double _lastKolvTemp = 78.0;
    private double _lastDestillatTemp = 82.0;
    private double _lastKylareTemp = 25.0;

    /// <summary>
    /// Konstruktor med optional Settings service
    /// </summary>
    public MockDataService(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Simulerar GetTemperaturesAsync från ApiService
    /// </summary>
    public async Task<TemperatureResponse?> GetTemperaturesAsync()
    {
        // Simulera nätverksfördröjning
        await Task.Delay(500);

        var sensors = await GenerateRealisticMockSensorsAsync();

        return new TemperatureResponse
        {
            SensorCount = sensors.Count,
            Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Sensors = sensors
        };
    }

    /// <summary>
    /// Genererar realistisk mock-data med Settings-baserade gränser
    /// </summary>
    private async Task<List<TemperatureReading>> GenerateRealisticMockSensorsAsync()
    {
        // Hämta temperaturinställningar
        var settings = await GetTemperatureSettingsAsync();

        // Gradvis förändring istället för helt slumpmässig
        _lastKolvTemp += (_random.NextDouble() * 2 - 1) * 0.5; // ±0.5°C förändring
        _lastDestillatTemp += (_random.NextDouble() * 2 - 1) * 0.3; // ±0.3°C förändring          
        _lastKylareTemp += (_random.NextDouble() * 2 - 1) * 0.2; // ±0.2°C förändring

        // Håll inom realistiska gränser
        _lastKolvTemp = Math.Max(65, Math.Min(95, _lastKolvTemp));
        _lastDestillatTemp = Math.Max(70, Math.Min(100, _lastDestillatTemp));
        _lastKylareTemp = Math.Max(15, Math.Min(45, _lastKylareTemp));

        return new List<TemperatureReading>
        {
            // Kolv - Använder Settings för gränser
            new TemperatureReading
            {
                Id = 0,
                Name = "Kolv",
                Temperature = Math.Round(_lastKolvTemp, 1),
                Status = GetStatusForTemperature(_lastKolvTemp, settings.KolvOptimalMin, settings.KolvWarningTemp, settings.KolvCriticalTemp),
                LedColor = GetLedColorForTemperature(_lastKolvTemp, settings.KolvOptimalMin, settings.KolvWarningTemp, settings.KolvCriticalTemp)
            },

            // Destillat - Använder Settings för gränser
            new TemperatureReading
            {
                Id = 1,
                Name = "Destillat",
                Temperature = Math.Round(_lastDestillatTemp, 1),
                Status = GetStatusForTemperature(_lastDestillatTemp, settings.DestillatOptimalMin, settings.DestillatWarningTemp, settings.DestillatCriticalTemp),
                LedColor = GetLedColorForTemperature(_lastDestillatTemp, settings.DestillatOptimalMin, settings.DestillatWarningTemp, settings.DestillatCriticalTemp)
            },

            // Kylare - Använder Settings för gränser
            new TemperatureReading
            {
                Id = 2,
                Name = "Kylare",
                Temperature = Math.Round(_lastKylareTemp, 1),
                Status = GetStatusForTemperature(_lastKylareTemp, settings.KylareOptimalMin, settings.KylareWarningTemp, settings.KylareCriticalTemp),
                LedColor = GetLedColorForTemperature(_lastKylareTemp, settings.KylareOptimalMin, settings.KylareWarningTemp, settings.KylareCriticalTemp)
            }
        };
    }

    /// <summary>
    /// Hämtar temperaturinställningar från Settings eller använder defaults
    /// </summary>
    private async Task<MockTemperatureSettings> GetTemperatureSettingsAsync()
    {
        if (_settingsService == null)
        {
            // Fallback till hårdkodade värden (samma som innan)
            return new MockTemperatureSettings
            {
                KolvOptimalMin = 70,
                KolvWarningTemp = 80,
                KolvCriticalTemp = 90,
                DestillatOptimalMin = 75,
                DestillatWarningTemp = 85,
                DestillatCriticalTemp = 95,
                KylareOptimalMin = 20,
                KylareWarningTemp = 30,
                KylareCriticalTemp = 40
            };
        }

        try
        {
            // Hämta från Settings (simulerar ESP32 som läser inställningar)
            return new MockTemperatureSettings
            {
                // Kolv inställningar
                KolvOptimalMin = await _settingsService.GetKolvOptimalMinAsync(),
                KolvWarningTemp = await _settingsService.GetKolvWarningTempAsync(),
                KolvCriticalTemp = await _settingsService.GetKolvCriticalTempAsync(),

                // Destillat inställningar
                DestillatOptimalMin = await _settingsService.GetDestillatOptimalMinAsync(),
                DestillatWarningTemp = await _settingsService.GetDestillatWarningTempAsync(),
                DestillatCriticalTemp = await _settingsService.GetDestillatCriticalTempAsync(),

                // Kylare inställningar
                KylareOptimalMin = await _settingsService.GetKylareOptimalMinAsync(),
                KylareWarningTemp = await _settingsService.GetKylareWarningTempAsync(),
                KylareCriticalTemp = await _settingsService.GetKylareCriticalTempAsync()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading temperature settings: {ex.Message}");
            // Fallback till defaults
            return await GetTemperatureSettingsAsync();
        }
    }

    /// <summary>
    /// Bestämmer status baserat på temperaturintervall
    /// </summary>
    private string GetStatusForTemperature(double temp, double optimalMin, double warningTemp, double criticalTemp)
    {
        return temp switch
        {
            var t when t < optimalMin => "cold",
            var t when t >= optimalMin && t < warningTemp => "optimal",
            var t when t >= warningTemp && t < criticalTemp => "warning",
            _ => "hot"
        };
    }

    /// <summary>
    /// Bestämmer LED-färg baserat på temperaturintervall
    /// </summary>
    private string GetLedColorForTemperature(double temp, double optimalMin, double warningTemp, double criticalTemp)
    {
        return temp switch
        {
            var t when t < optimalMin => "blue",      // För kallt
            var t when t >= optimalMin && t < warningTemp => "green",   // Optimal
            var t when t >= warningTemp && t < criticalTemp => "yellow", // Varning
            _ => "red"  // Kritisk
        };
    }
}

/// <summary>
/// Helper class för temperaturinställningar
/// </summary>
public class MockTemperatureSettings
{
    // Kolv
    public double KolvOptimalMin { get; set; }
    public double KolvWarningTemp { get; set; }
    public double KolvCriticalTemp { get; set; }

    // Destillat
    public double DestillatOptimalMin { get; set; }
    public double DestillatWarningTemp { get; set; }
    public double DestillatCriticalTemp { get; set; }

    // Kylare
    public double KylareOptimalMin { get; set; }
    public double KylareWarningTemp { get; set; }
    public double KylareCriticalTemp { get; set; }
}
