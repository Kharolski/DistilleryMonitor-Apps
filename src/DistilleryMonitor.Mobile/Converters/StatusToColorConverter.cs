using System.Globalization;

namespace DistilleryMonitor.Mobile.Converters;

/// <summary>
/// Konverterar temperaturstatus till färg
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            // Kolla om vi vill ha kort-bakgrund eller LED-färg
            bool isCardBackground = parameter?.ToString() == "card";

            return status.ToLower() switch
            {
                "optimal" => isCardBackground ? Color.FromArgb("#2d5a3d") : Color.FromArgb("#00d4aa"),    // Mörk grön / Ljus grön
                "warning" => isCardBackground ? Color.FromArgb("#5a4a2d") : Color.FromArgb("#ffa726"),   // Mörk gul / Ljus gul  
                "hot" => isCardBackground ? Color.FromArgb("#5a2d2d") : Color.FromArgb("#ff6b6b"),       // Mörk röd / Ljus röd
                "cold" => isCardBackground ? Color.FromArgb("#2d3d5a") : Color.FromArgb("#42a5f5"),     // Mörk blå / Ljus blå
                _ => isCardBackground ? Color.FromArgb("#16213e") : Color.FromArgb("#888888")            // Default mörk / Grå
            };
        }
        return Color.FromArgb("#16213e"); // Default
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
