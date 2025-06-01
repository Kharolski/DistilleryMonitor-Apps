namespace DistilleryMonitor.Core.Models
{

    /*
            Från ESP32 API-svar:

    {
      "id": 0,              ← Id property
      "name": "Kolv",       ← Name property  
      "temperature": 65.3,  ← Temperature property
      "status": "optimal",  ← Status property
      "led_color": "green"  ← LedColor property
    }

    */

    public class TemperatureReading
    {
        public int Id { get; set; }                             // Sensor nummer (0, 1, 2)
        public string Name { get; set; } = string.Empty;        // "Kolv", "Destillat", "Kylare"  
        public double Temperature { get; set; }                 // Aktuell temperatur (65.3)
        public string Status { get; set; } = string.Empty;      // "optimal", "warning", "cold"
        public string LedColor { get; set; } = string.Empty;    // "green", "yellow", "blue", "red"
    }
}
