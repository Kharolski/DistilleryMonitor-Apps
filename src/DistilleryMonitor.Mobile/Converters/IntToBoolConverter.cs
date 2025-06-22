using System.Globalization;

namespace DistilleryMonitor.Mobile.Converters
{
    /// <summary>
    /// Konverterar int till bool
    /// - 0 = false (dölj)
    /// - >0 = true (visa)
    /// Med parameter "true" = inverterar resultatet
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;

            if (value is int intValue)
            {
                result = intValue > 0;
            }
            else if (value is string stringValue && int.TryParse(stringValue, out int parsedValue))
            {
                result = parsedValue > 0;
            }

            // Om parameter är "true" eller "invert", invertera resultatet
            if (parameter?.ToString()?.ToLower() == "true" ||
                parameter?.ToString()?.ToLower() == "invert")
            {
                result = !result;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack behövs sällan för denna converter
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }
            return 0;
        }
    }
}
