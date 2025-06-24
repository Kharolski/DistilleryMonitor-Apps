namespace DistilleryMonitor.Core.Models
{
    /// <summary>
    /// Temperaturstatus som matchar ESP32 API värden
    /// </summary>
    public enum TemperatureStatus
    {
        TooLow,     // "cold" från ESP32
        Optimal,    // "optimal" från ESP32  
        Warning,    // "warning" från ESP32
        Critical    // "critical" från ESP32 
    }
}
