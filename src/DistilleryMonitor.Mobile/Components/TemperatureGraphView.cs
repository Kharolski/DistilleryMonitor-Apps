using Microsoft.Maui.Graphics;
using DistilleryMonitor.Core.Services;

namespace DistilleryMonitor.Mobile.Components;

public class TemperatureGraphView : GraphicsView, IDrawable
{
    public static readonly BindableProperty TemperatureProperty =
        BindableProperty.Create(nameof(Temperature), typeof(double), typeof(TemperatureGraphView),
            propertyChanged: OnTemperatureChanged);

    public static readonly BindableProperty SensorNameProperty =
        BindableProperty.Create(nameof(SensorName), typeof(string), typeof(TemperatureGraphView),
            propertyChanged: OnSensorNameChanged);

    public static readonly BindableProperty SettingsServiceProperty =
        BindableProperty.Create(nameof(SettingsService), typeof(ISettingsService), typeof(TemperatureGraphView));

    public ISettingsService SettingsService
    {
        get => (ISettingsService)GetValue(SettingsServiceProperty);
        set => SetValue(SettingsServiceProperty, value);
    }

    public double Temperature
    {
        get => (double)GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, value);
    }

    public string SensorName
    {
        get => (string)GetValue(SensorNameProperty);
        set => SetValue(SensorNameProperty, value);
    }

    private readonly List<TemperaturePoint> _temperatureHistory = new();
    private const int MAX_POINTS = 100; // 100 mätningar innan raderas det gamla (mer data)

    // Cache för temperaturinställningar
    private double _cachedOptimalMin = 50.0;
    private double _cachedWarningTemp = 80.0;
    private double _cachedCriticalTemp = 90.0;
    private bool _settingsLoaded = false;


    public TemperatureGraphView()
    {
        Drawable = this;
        BackgroundColor = Color.FromArgb("#333333");
    }

    private static void OnTemperatureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureGraphView view && newValue is double temp)
        {
            view.AddTemperature(temp);
        }
    }

    private static  async void OnSensorNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureGraphView view)
        {
            await view.LoadTemperatureSettingsAsync();
            view.Invalidate();
        }
    }

    public async Task RefreshSettingsAsync()
    {
        await LoadTemperatureSettingsAsync();
        Invalidate(); // Tvingar grafen att ritas om
    }

    // Ladda och cacha temperaturinställningar
    private async Task LoadTemperatureSettingsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SensorName))
                return;

            // Lägg till debug-utskrift
            System.Diagnostics.Debug.WriteLine($"Laddar temperaturinställningar för {SensorName}...");

            if (SettingsService != null)
            {
                // Försök hämta från SettingsService först
                _cachedOptimalMin = SensorName switch
                {
                    "Kolv" => await SettingsService.GetKolvOptimalMinAsync(),
                    "Destillat" => await SettingsService.GetDestillatOptimalMinAsync(),
                    "Kylare" => await SettingsService.GetKylareOptimalMinAsync(),
                    _ => GetFallbackOptimalMin(SensorName)
                };

                _cachedWarningTemp = SensorName switch
                {
                    "Kolv" => await SettingsService.GetKolvWarningTempAsync(),
                    "Destillat" => await SettingsService.GetDestillatWarningTempAsync(),
                    "Kylare" => await SettingsService.GetKylareWarningTempAsync(),
                    _ => GetFallbackWarningTemp(SensorName)
                };

                _cachedCriticalTemp = SensorName switch
                {
                    "Kolv" => await SettingsService.GetKolvCriticalTempAsync(),
                    "Destillat" => await SettingsService.GetDestillatCriticalTempAsync(),
                    "Kylare" => await SettingsService.GetKylareCriticalTempAsync(),
                    _ => GetFallbackCriticalTemp(SensorName)
                };
            }
            else
            {
                // Fallback till Preferences om SettingsService inte finns
                _cachedOptimalMin = await GetOptimalMinTempAsync(SensorName);
                _cachedWarningTemp = await GetWarningTempAsync(SensorName);
                _cachedCriticalTemp = await GetCriticalTempAsync(SensorName);
            }

            _settingsLoaded = true;

            // Debug-utskrift med värden
            System.Diagnostics.Debug.WriteLine($"Laddade inställningar för {SensorName}: Optimal={_cachedOptimalMin}, Warning={_cachedWarningTemp}, Critical={_cachedCriticalTemp}");
        }
        catch (Exception ex)
        {
            // Fallback vid fel
            _cachedOptimalMin = GetFallbackOptimalMin(SensorName);
            _cachedWarningTemp = GetFallbackWarningTemp(SensorName);
            _cachedCriticalTemp = GetFallbackCriticalTemp(SensorName);
            _settingsLoaded = true;

            System.Diagnostics.Debug.WriteLine($"Fel vid laddning av temperaturinställningar: {ex.Message}");
        }
    }

    private void AddTemperature(double temp)
    {
        _temperatureHistory.Add(new TemperaturePoint
        {
            Temperature = temp,
            Timestamp = DateTime.Now
        });

        // Rensa gamla data (äldre än 10 minuter)
        var cutoffTime = DateTime.Now.AddMinutes(-10);
        _temperatureHistory.RemoveAll(point => point.Timestamp < cutoffTime);

        // Säkerhetsgräns
        if (_temperatureHistory.Count > MAX_POINTS)
        {
            _temperatureHistory.RemoveRange(0, _temperatureHistory.Count - MAX_POINTS);
        }

        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Rensa bakgrund
        canvas.FillColor = Color.FromArgb("#333333");
        canvas.FillRectangle(dirtyRect);

        // Titel
        canvas.FontColor = Colors.White;
        canvas.FontSize = 16;
        canvas.DrawString($"📈 {SensorName ?? "Sensor"}",
            dirtyRect.Center.X,
            dirtyRect.Top + 25,
            HorizontalAlignment.Center);

        // Rita graf om vi har data
        if (_temperatureHistory.Count > 1 && _settingsLoaded)
        {
            var graphArea = new RectF(
                dirtyRect.Left + 40,
                dirtyRect.Top + 50,
                dirtyRect.Width - 75,
                dirtyRect.Height - 100);

            DrawGraphBackground(canvas, graphArea);

            // Använd cachade värden istället för async
            var minTemp = Math.Max(0, _cachedOptimalMin - 5);
            var maxTemp = Math.Min(100, _cachedCriticalTemp + 5);
            var tempRange = maxTemp - minTemp;

            // Rita i rätt ordning
            DrawGrid(canvas, graphArea, minTemp, maxTemp);
            DrawTemperatureReferenceLines(canvas, graphArea, minTemp, tempRange,
                _cachedOptimalMin, _cachedWarningTemp, _cachedCriticalTemp);
            DrawTemperatureLine(canvas, graphArea, minTemp, tempRange);
            DrawAxes(canvas, graphArea, minTemp, maxTemp);
        }
        else
        {
            // Placeholder 
            canvas.FontSize = 14;
            canvas.FontColor = Colors.White;
            var message = _settingsLoaded ? "Samlar temperaturdata..." : "Laddar inställningar...";
            canvas.DrawString(message,
                dirtyRect.Center.X,
                dirtyRect.Center.Y,
                HorizontalAlignment.Center);
        }
    }

    private async Task<double> GetOptimalMinTempAsync(string sensorName)
    {
        if (SettingsService == null)
            return GetFallbackOptimalMin(sensorName);

        try
        {
            return sensorName switch
            {
                "Kolv" => await SettingsService.GetKolvOptimalMinAsync(),
                "Destillat" => await SettingsService.GetDestillatOptimalMinAsync(),
                "Kylare" => await SettingsService.GetKylareOptimalMinAsync(),
                _ => 50.0
            };
        }
        catch
        {
            return GetFallbackOptimalMin(sensorName);
        }
    }

    private async Task<double> GetWarningTempAsync(string sensorName)
    {
        if (SettingsService == null)
            return GetFallbackWarningTemp(sensorName);

        try
        {
            return sensorName switch
            {
                "Kolv" => await SettingsService.GetKolvWarningTempAsync(),
                "Destillat" => await SettingsService.GetDestillatWarningTempAsync(),
                "Kylare" => await SettingsService.GetKylareWarningTempAsync(),
                _ => 80.0
            };
        }
        catch
        {
            return GetFallbackWarningTemp(sensorName);
        }
    }

    private async Task<double> GetCriticalTempAsync(string sensorName)
    {
        if (SettingsService == null)
            return GetFallbackCriticalTemp(sensorName);

        try
        {
            return sensorName switch
            {
                "Kolv" => await SettingsService.GetKolvCriticalTempAsync(),
                "Destillat" => await SettingsService.GetDestillatCriticalTempAsync(),
                "Kylare" => await SettingsService.GetKylareCriticalTempAsync(),
                _ => 90.0
            };
        }
        catch
        {
            return GetFallbackCriticalTemp(sensorName);
        }
    }

    private double GetFallbackOptimalMin(string sensorName)
    {
        return sensorName switch
        {
            "Kolv" => 70.0,
            "Destillat" => 75.0,
            "Kylare" => 20.0,
            _ => 50.0
        };
    }

    private double GetFallbackWarningTemp(string sensorName)
    {
        return sensorName switch
        {
            "Kolv" => 80.0,
            "Destillat" => 85.0,
            "Kylare" => 30.0,
            _ => 80.0
        };
    }

    private double GetFallbackCriticalTemp(string sensorName)
    {
        return sensorName switch
        {
            "Kolv" => 90.0,
            "Destillat" => 95.0,
            "Kylare" => 40.0,
            _ => 90.0
        };
    }

    /// <summary>
    /// Rita ljus graf-bakgrund med skugga
    /// </summary>
    private void DrawGraphBackground(ICanvas canvas, RectF area)
    {
        // Skugga
        var shadowArea = new RectF(area.X + 3, area.Y + 3, area.Width, area.Height);
        canvas.FillColor = Color.FromArgb("#000000").WithAlpha(0.3f);
        canvas.FillRoundedRectangle(shadowArea, 8);

        // Ljus bakgrund
        canvas.FillColor = Color.FromArgb("#f8f9fa"); // Ljusgrå/vit
        canvas.FillRoundedRectangle(area, 8);

        // Svart kant
        canvas.StrokeColor = Color.FromArgb("#333333");
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(area, 8);
    }

    /// <summary>
    /// Rita temperatur-referenslinjer
    /// </summary>
    private void DrawTemperatureReferenceLines(ICanvas canvas, RectF area, double minTemp, double tempRange,
    double optimalMin, double warningTemp, double criticalTemp)
    {
        canvas.StrokeSize = 2;
        canvas.StrokeDashPattern = new float[] { 8, 4 };

        // Optimal börjar vid - Grön
        if (optimalMin >= minTemp && optimalMin <= minTemp + tempRange)
        {
            var y = (float)(area.Bottom - (optimalMin - minTemp) / tempRange * area.Height);
            canvas.StrokeColor = Color.FromArgb("#28a745");
            canvas.DrawLine(area.Left, y, area.Right, y);

            canvas.FontColor = Color.FromArgb("#28a745");
            canvas.FontSize = 12;
            canvas.DrawString($"{optimalMin:F1}°", area.Right + 5, y, HorizontalAlignment.Left);
        }

        // Varning börjar vid - Gul/Orange
        if (warningTemp >= minTemp && warningTemp <= minTemp + tempRange)
        {
            var y = (float)(area.Bottom - (warningTemp - minTemp) / tempRange * area.Height);
            canvas.StrokeColor = Color.FromArgb("#ffc107");
            canvas.DrawLine(area.Left, y, area.Right, y);

            canvas.FontColor = Color.FromArgb("#ffc107");
            canvas.FontSize = 12;
            canvas.DrawString($"{warningTemp:F1}°", area.Right + 5, y, HorizontalAlignment.Left);
        }

        // Kritisk börjar vid - Röd
        if (criticalTemp >= minTemp && criticalTemp <= minTemp + tempRange)
        {
            var y = (float)(area.Bottom - (criticalTemp - minTemp) / tempRange * area.Height);
            canvas.StrokeColor = Color.FromArgb("#dc3545");
            canvas.DrawLine(area.Left, y, area.Right, y);

            canvas.FontColor = Color.FromArgb("#dc3545");
            canvas.FontSize = 12;
            canvas.DrawString($"{criticalTemp:F1}°", area.Right + 5, y, HorizontalAlignment.Left);
        }

        canvas.StrokeDashPattern = null;
    }

    /// <summary>
    /// Rita rutnät (ljusare på ljus bakgrund)
    /// </summary>
    private void DrawGrid(ICanvas canvas, RectF area, double minTemp, double maxTemp)
    {
        canvas.StrokeColor = Color.FromArgb("#e0e0e0"); // Ljusgrå rutnät
        canvas.StrokeSize = 1;

        var tempRange = maxTemp - minTemp;
        var gridInterval = tempRange switch
        {
            <= 20 => 0.5,
            <= 40 => 1.0,
            <= 60 => 2.0,
            _ => 5.0
        };

        // Horisontella linjer
        var startTemp = Math.Ceiling(minTemp / gridInterval) * gridInterval;
        for (double temp = startTemp; temp <= maxTemp; temp += gridInterval)
        {
            var y = (float)(area.Bottom - (temp - minTemp) / tempRange * area.Height);
            canvas.DrawLine(area.Left, y, area.Right, y);
        }

        // Vertikala linjer (tid)
        for (int i = 0; i <= 4; i++)
        {
            var x = area.Left + (i * area.Width / 4);
            canvas.DrawLine(x, area.Top, x, area.Bottom);
        }
    }

    /// <summary>
    /// Rita temperaturlinje (smart färg på ljus bakgrund)
    /// </summary>
    private void DrawTemperatureLine(ICanvas canvas, RectF area, double minTemp, double tempRange)
    {
        canvas.StrokeSize = 3;      // 3 pixlar linje:
        canvas.StrokeLineCap = LineCap.Square;

        if (_temperatureHistory.Count < 2)
            return;

        // Använd första punktens tid som start istället för "nu minus 5 min"
        var firstPointTime = _temperatureHistory.First().Timestamp;
        var lastPointTime = _temperatureHistory.Last().Timestamp;

        // Minst 5 minuters span, men börja från första punkten
        var timeSpan = Math.Max(5.0, (lastPointTime - firstPointTime).TotalMinutes);

        // Rita alla punkter
        for (int i = 1; i < _temperatureHistory.Count; i++)
        {
            var temp1 = _temperatureHistory[i - 1].Temperature;
            var temp2 = _temperatureHistory[i].Temperature;
            var time1 = _temperatureHistory[i - 1].Timestamp;
            var time2 = _temperatureHistory[i].Timestamp;

            // Beräkna position från första punkten
            var minutes1FromStart = (time1 - firstPointTime).TotalMinutes;
            var minutes2FromStart = (time2 - firstPointTime).TotalMinutes;

            // Konvertera till X-koordinater
            var x1 = (float)(area.Left + (minutes1FromStart / timeSpan) * area.Width);
            var x2 = (float)(area.Left + (minutes2FromStart / timeSpan) * area.Width);

            // Y-position som vanligt
            var y1 = CalculateYPosition(temp1, minTemp, tempRange, area);
            var y2 = CalculateYPosition(temp2, minTemp, tempRange, area);

            // Rita bara linjen
            canvas.StrokeColor = Color.FromArgb("#000000");
            canvas.DrawLine(x1, y1, x2, y2);
        }

    }

    /// <summary>
    /// Beräkna Y-position med begränsning till graf-området
    /// </summary>
    private float CalculateYPosition(double temperature, double minTemp, double tempRange, RectF area)
    {
        // Begränsa temperatur till graf-range
        var clampedTemp = Math.Max(minTemp, Math.Min(minTemp + tempRange, temperature));

        // Beräkna Y-position
        var normalizedPosition = (clampedTemp - minTemp) / tempRange;
        var y = area.Bottom - (normalizedPosition * area.Height);

        // Extra säkerhet - begränsa till graf-området
        return (float)Math.Max(area.Top, Math.Min(area.Bottom, y));
    }

    /// <summary>
    /// Rita axlar (fetare på ljus bakgrund)
    /// </summary>
    private void DrawAxes(ICanvas canvas, RectF area, double minTemp, double maxTemp)
    {
        // Fetare axlar med skugga
        canvas.StrokeColor = Color.FromArgb("#666666");
        canvas.StrokeSize = 3;

        // Y-axel
        canvas.DrawLine(area.Left, area.Top, area.Left, area.Bottom);

        // X-axel
        canvas.DrawLine(area.Left, area.Bottom, area.Right, area.Bottom);

        // Labels (mörka på ljus bakgrund)
        canvas.FontColor = Colors.White;
        canvas.FontSize = 12;

        var tempRange = maxTemp - minTemp;
        var labelInterval = tempRange switch
        {
            <= 20 => 2.0,
            <= 40 => 5.0,
            <= 60 => 10.0,
            _ => 20.0
        };

        // Y-axel labels (temperatur)
        var startTemp = Math.Ceiling(minTemp / labelInterval) * labelInterval;
        for (double temp = startTemp; temp <= maxTemp; temp += labelInterval)
        {
            var y = (float)(area.Bottom - (temp - minTemp) / tempRange * area.Height);
            canvas.DrawString($"{temp:F0}°C", area.Left - 25, y, HorizontalAlignment.Center);
        }

        // X-axel labels (tid) - Rullande 15-minuters fönster
        // X-axel labels (tid) - Från första punkten
        canvas.FontColor = Colors.White;
        canvas.FontSize = 10;

        if (_temperatureHistory.Count > 0)
        {
            var firstTime = _temperatureHistory.First().Timestamp;
            var lastTime = _temperatureHistory.Last().Timestamp;
            var totalMinutes = Math.Max(5.0, (lastTime - firstTime).TotalMinutes);

            for (int i = 0; i <= 5; i++)
            {
                var displayTime = firstTime.AddMinutes(i * totalMinutes / 5);
                var x = area.Left + (i * area.Width / 5);
                var timeText = displayTime.ToString("HH:mm");
                canvas.DrawString(timeText, x, area.Bottom + 20, HorizontalAlignment.Center);
            }
        }
    }

    // Hjälpklass för att lagra temperatur med tid
    private class TemperaturePoint
    {
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
