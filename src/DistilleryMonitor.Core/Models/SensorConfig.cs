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

    public class SensorConfig
    {
        public double BlueLimit { get; set; }               // blå LED
        public double GreenLimit { get; set; }              // grön LED  
        public double YellowLimit { get; set; }             // gul LED
        public string Name { get; set; } = string.Empty;    // Sensor namn
    }
}
