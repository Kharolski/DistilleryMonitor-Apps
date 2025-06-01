namespace DistilleryMonitor.Core.Models
{
    /*
            Matchar ESP32 API-struktur:

    {
      "sensor0": {
        "blue_limit": 50.0,   ← BlueLimit
        "green_limit": 70.0,  ← GreenLimit  
        "yellow_limit": 80.0, ← YellowLimit
        "name": "Kolv"        ← Name
      }
    }

    */

    public class ConfigurationResponse
    {
        public SensorConfig Sensor0 { get; set; } = new();
        public SensorConfig Sensor1 { get; set; } = new();
        public SensorConfig Sensor2 { get; set; } = new();
    }
}
