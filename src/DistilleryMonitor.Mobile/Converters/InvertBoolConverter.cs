using System.Globalization;

namespace DistilleryMonitor.Mobile.Converters;

/// <summary>
/// Inverterar bool-värde - true blir false, false blir true
/// Används för att inaktivera knappen när IsLoading = true
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue; // Inverterar värdet
        }
        return true; // Default: aktiverad
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
