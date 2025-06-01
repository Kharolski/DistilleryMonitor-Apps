namespace DistilleryMonitor.Core.Models
{

    /*
            Matchar ESP32 API-struktur:

    {
        "sensors": [...],           ← List<TemperatureReading> Sensors
        "timestamp": 1234567890,    ← long Timestamp  
        "sensor_count": 3           ← int SensorCount
    }

    */

    public class TemperatureResponse
    {
        public List<TemperatureReading> Sensors { get; set; } = new();      // = new() = Skapar tom lista som default (C# 9+ syntax)
        public long Timestamp { get; set; }
        public int SensorCount { get; set; }
    }
}
