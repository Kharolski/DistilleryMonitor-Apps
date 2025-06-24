namespace DistilleryMonitor.Core.Models;

/// <summary>
/// Temperaturinställningar för en sensor (app:ens struktur)
/// Konverteras till/från ESP32:s BlueLimit/GreenLimit/YellowLimit
/// </summary>
public class SensorSettings
{
    public string SensorName { get; set; } = string.Empty;
    public double OptimalMin { get; set; }    // → ESP32 BlueLimit
    public double WarningTemp { get; set; }   // → ESP32 GreenLimit  
    public double CriticalTemp { get; set; }  // → ESP32 YellowLimit
}
