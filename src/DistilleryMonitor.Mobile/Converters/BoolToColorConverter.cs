using System.Globalization;

namespace DistilleryMonitor.Mobile.Converters;

/// <summary>
/// Konverterar bool till färg för anslutningsstatus
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Color.FromArgb("#00d4aa") : Color.FromArgb("#ff6b6b");
            //                   ↑ Grön (ansluten)      ↑ Röd (frånkopplad)
        }
        return Color.FromArgb("#888888"); // Grå (okänd status)
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
