using System.Globalization;

namespace DistilleryMonitor.Mobile.Converters;

public class MockDataToSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool useMockData = (bool)(value ?? false);
        return useMockData ? "🧪 Testdata" : "📡 ESP32";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
