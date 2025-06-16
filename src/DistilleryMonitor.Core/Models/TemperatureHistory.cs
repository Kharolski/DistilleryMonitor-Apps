namespace DistilleryMonitor.Core.Models
{
    public class TemperatureHistory
    {
        public int Id { get; set; }
        public string SensorName { get; set; } = string.Empty;
        public int SensorId { get; set; }
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }

        // Hjälp-properties för UI
        public string FormattedTemperature => $"{Temperature:F1}°C";
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        public string FormattedDate => Timestamp.ToString("yyyy-MM-dd");
    }
}
