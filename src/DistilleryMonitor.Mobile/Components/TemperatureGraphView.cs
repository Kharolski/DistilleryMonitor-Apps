using Microsoft.Maui.Graphics;
using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile.Components;

public class TemperatureGraphView : GraphicsView, IDrawable
{
    #region Bindable Properties
    public static readonly BindableProperty TemperatureProperty =
        BindableProperty.Create(nameof(Temperature), typeof(double), typeof(TemperatureGraphView),
            propertyChanged: OnTemperatureChanged);

    public static readonly BindableProperty SensorNameProperty =
        BindableProperty.Create(nameof(SensorName), typeof(string), typeof(TemperatureGraphView),
            propertyChanged: OnSensorNameChanged);

    public static readonly BindableProperty SettingsServiceProperty =
        BindableProperty.Create(nameof(SettingsService), typeof(ISettingsService), typeof(TemperatureGraphView));

    public static readonly BindableProperty ThresholdServiceProperty =
    BindableProperty.Create(nameof(ThresholdService), typeof(TemperatureThresholdService), typeof(TemperatureGraphView),
        propertyChanged: OnThresholdServiceChanged);

    public ISettingsService SettingsService
    {
        get => (ISettingsService)GetValue(SettingsServiceProperty);
        set => SetValue(SettingsServiceProperty, value);
    }

    public TemperatureThresholdService ThresholdService
    {
        get => (TemperatureThresholdService)GetValue(ThresholdServiceProperty);
        set => SetValue(ThresholdServiceProperty, value);
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
    #endregion

    #region Private Fields
    private readonly List<TemperaturePoint> _temperatureHistory = new();
    private const int MAX_POINTS = 100; // 100 mätningar innan raderas det gamla (mer data)

    // Cache för temperaturinställningar
    private double _cachedOptimalMin = 50.0;
    private double _cachedWarningTemp = 80.0;
    private double _cachedCriticalTemp = 90.0;
    private bool _settingsLoaded = false;
    #endregion

    #region Constructor
    public TemperatureGraphView()
    {
        Drawable = this;
        BackgroundColor = Color.FromArgb("#333333");

        // Registrera för settings-ändringar när både services är tillgängliga
        PropertyChanged += OnPropertyChanged;
    }
    #endregion

    #region Property Changed Events
    private static void OnTemperatureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureGraphView view && newValue is double temp)
        {
            view.AddTemperature(temp);
        }
    }

    private static async void OnSensorNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureGraphView view)
        {
            await view.LoadTemperatureSettingsAsync();
            view.Invalidate();
        }
    }

    // Event handler för ThresholdService
    private static void OnThresholdServiceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureGraphView view && view.SettingsService != null)
        {
            // Vi behöver bara veta att ThresholdService har ändrats
            // Eventet kommer fortfarande från SettingsService
            System.Diagnostics.Debug.WriteLine($"🔄 ThresholdService kopplat till graf för {view.SensorName}");

            // Registrera för settings-ändringar
            view.RegisterForSettingsChanges();

            // Ladda om inställningar när ThresholdService kopplas
            _ = Task.Run(async () =>
            {
                await view.LoadTemperatureSettingsAsync();
                MainThread.BeginInvokeOnMainThread(() => view.Invalidate());
            });
        }
    }

    // Hantera när properties ändras
    private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService) || e.PropertyName == nameof(ThresholdService))
        {
            RegisterForSettingsChanges();
        }
    }

    // Registrera för settings-ändringar
    private void RegisterForSettingsChanges()
    {
        if (SettingsService != null && ThresholdService != null)
        {
            // Avregistrera först (för säkerhets skull)
            SettingsService.TemperatureSettingsChanged -= OnTemperatureSettingsChanged;

            // Registrera för ändringar
            SettingsService.TemperatureSettingsChanged += OnTemperatureSettingsChanged;

            System.Diagnostics.Debug.WriteLine($"✅ Graf registrerad för settings-ändringar: {SensorName}");
        }
    }
    #endregion

    #region Event Handlers
    // Event handler som triggas när settings ändras
    private async void OnTemperatureSettingsChanged(object sender, TemperatureSettingsChangedEventArgs e)
    {
        // Bara uppdatera om det är vår sensor
        if (e.SensorName == SensorName)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Graf uppdateras för {SensorName} - nya värden: Optimal={e.OptimalMin}, Warning={e.WarningTemp}, Critical={e.CriticalTemp}");

            // Uppdatera cache direkt från event
            _cachedOptimalMin = e.OptimalMin;
            _cachedWarningTemp = e.WarningTemp;
            _cachedCriticalTemp = e.CriticalTemp;

            // Rita om grafen på UI-tråden
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Invalidate();
            });
        }
    }
    #endregion

    #region Public Methods
    public async Task RefreshSettingsAsync()
    {
        await LoadTemperatureSettingsAsync();
        Invalidate(); // Tvingar grafen att ritas om
    }
    #endregion

    #region Data Management
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

    // VISA SENASTE 40% AV DATAN
    private List<TemperaturePoint> GetDisplayData()
    {
        if (_temperatureHistory.Count <= 5)
            return _temperatureHistory; // Visa allt om vi har lite data

        // Beräkna hur många punkter som ska visas (senaste 40%)
        var pointsToShow = Math.Max(5, (int)(_temperatureHistory.Count * 0.4));

        // Ta de senaste punkterna
        return _temperatureHistory
            .Skip(_temperatureHistory.Count - pointsToShow)
            .ToList();
    }
    #endregion

    #region Settings Management
    private async Task LoadTemperatureSettingsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SensorName))
                return;

            System.Diagnostics.Debug.WriteLine($"Laddar temperaturinställningar för {SensorName}...");

            if (SettingsService != null)
            {
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
                _cachedOptimalMin = await GetOptimalMinTempAsync(SensorName);
                _cachedWarningTemp = await GetWarningTempAsync(SensorName);
                _cachedCriticalTemp = await GetCriticalTempAsync(SensorName);
            }

            _settingsLoaded = true;
            System.Diagnostics.Debug.WriteLine($"Laddade inställningar för {SensorName}: Optimal={_cachedOptimalMin}, Warning={_cachedWarningTemp}, Critical={_cachedCriticalTemp}");
        }
        catch (Exception ex)
        {
            _cachedOptimalMin = GetFallbackOptimalMin(SensorName);
            _cachedWarningTemp = GetFallbackWarningTemp(SensorName);
            _cachedCriticalTemp = GetFallbackCriticalTemp(SensorName);
            _settingsLoaded = true;
            System.Diagnostics.Debug.WriteLine($"Fel vid laddning av temperaturinställningar: {ex.Message}");
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
    #endregion

    #region Fallback Settings
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
    #endregion

    #region Drawing Methods
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#333333");
        canvas.FillRectangle(dirtyRect);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 16;
        canvas.DrawString($"📈 {SensorName ?? "Sensor"}",
            dirtyRect.Center.X,
            dirtyRect.Top + 25,
            HorizontalAlignment.Center);

        // Använd display data istället för full historia
        var displayData = GetDisplayData();

        if (displayData.Count > 1 && _settingsLoaded)
        {
            var graphArea = new RectF(
                dirtyRect.Left + 40,
                dirtyRect.Top + 50,
                dirtyRect.Width - 75,
                dirtyRect.Height - 100);

            DrawGraphBackground(canvas, graphArea);

            var minTemp = Math.Max(0, _cachedOptimalMin - 5);
            var maxTemp = Math.Min(100, _cachedCriticalTemp + 5);
            var tempRange = maxTemp - minTemp;

            DrawGrid(canvas, graphArea, minTemp, maxTemp);
            DrawTemperatureReferenceLines(canvas, graphArea, minTemp, tempRange,
                _cachedOptimalMin, _cachedWarningTemp, _cachedCriticalTemp);
            DrawTemperatureLine(canvas, graphArea, minTemp, tempRange, displayData); // Skicka display data
            DrawAxes(canvas, graphArea, minTemp, maxTemp, displayData); // Skicka display data
        }
        else
        {
            canvas.FontSize = 14;
            canvas.FontColor = Colors.White;
            var message = _settingsLoaded ? "Samlar temperaturdata..." : "Laddar inställningar...";
            canvas.DrawString(message,
                dirtyRect.Center.X,
                dirtyRect.Center.Y,
                HorizontalAlignment.Center);
        }
    }

    private void DrawGraphBackground(ICanvas canvas, RectF area)
    {
        var shadowArea = new RectF(area.X + 3, area.Y + 3, area.Width, area.Height);
        canvas.FillColor = Color.FromArgb("#000000").WithAlpha(0.3f);
        canvas.FillRoundedRectangle(shadowArea, 8);

        canvas.FillColor = Color.FromArgb("#f8f9fa");
        canvas.FillRoundedRectangle(area, 8);

        canvas.StrokeColor = Color.FromArgb("#333333");
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(area, 8);
    }

    private void DrawTemperatureReferenceLines(ICanvas canvas, RectF area, double minTemp, double tempRange,
        double optimalMin, double warningTemp, double criticalTemp)
    {
        canvas.StrokeSize = 2;
        canvas.StrokeDashPattern = new float[] { 8, 4 };

        if (optimalMin >= minTemp && optimalMin <= minTemp + tempRange)
        {
            var y = (float)(area.Bottom - (optimalMin - minTemp) / tempRange * area.Height);
            canvas.StrokeColor = Color.FromArgb("#28a745");
            canvas.DrawLine(area.Left, y, area.Right, y);
            canvas.FontColor = Color.FromArgb("#28a745");
            canvas.FontSize = 12;
            canvas.DrawString($"{optimalMin:F1}°", area.Right + 5, y, HorizontalAlignment.Left);
        }

        if (warningTemp >= minTemp && warningTemp <= minTemp + tempRange)
        {
            var y = (float)(area.Bottom - (warningTemp - minTemp) / tempRange * area.Height);
            canvas.StrokeColor = Color.FromArgb("#ffc107");
            canvas.DrawLine(area.Left, y, area.Right, y);
            canvas.FontColor = Color.FromArgb("#ffc107");
            canvas.FontSize = 12;
            canvas.DrawString($"{warningTemp:F1}°", area.Right + 5, y, HorizontalAlignment.Left);
        }

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

    private void DrawGrid(ICanvas canvas, RectF area, double minTemp, double maxTemp)
    {
        canvas.StrokeColor = Color.FromArgb("#e0e0e0");
        canvas.StrokeSize = 1;
        var tempRange = maxTemp - minTemp;
        var gridInterval = tempRange switch
        {
            <= 20 => 0.5,
            <= 40 => 1.0,
            <= 60 => 2.0,
            _ => 5.0
        };

        var startTemp = Math.Ceiling(minTemp / gridInterval) * gridInterval;
        for (double temp = startTemp; temp <= maxTemp; temp += gridInterval)
        {
            var y = (float)(area.Bottom - (temp - minTemp) / tempRange * area.Height);
            canvas.DrawLine(area.Left, y, area.Right, y);
        }

        for (int i = 0; i <= 4; i++)
        {
            var x = area.Left + (i * area.Width / 4);
            canvas.DrawLine(x, area.Top, x, area.Bottom);
        }
    }

    // Uppdaterad temperaturlinje med display data
    private void DrawTemperatureLine(ICanvas canvas, RectF area, double minTemp, double tempRange, List<TemperaturePoint> displayData)
    {
        canvas.StrokeSize = 3;
        canvas.StrokeLineCap = LineCap.Square;

        if (displayData.Count < 2)
            return;

        // Använd display data istället för full historia
        var firstPointTime = displayData.First().Timestamp;
        var lastPointTime = displayData.Last().Timestamp;

        var timeSpan = Math.Max(5.0, (lastPointTime - firstPointTime).TotalMinutes);

        // Rita alla punkter från display data
        for (int i = 1; i < displayData.Count; i++)
        {
            var temp1 = displayData[i - 1].Temperature;
            var temp2 = displayData[i].Temperature;
            var time1 = displayData[i - 1].Timestamp;
            var time2 = displayData[i].Timestamp;

            var minutes1FromStart = (time1 - firstPointTime).TotalMinutes;
            var minutes2FromStart = (time2 - firstPointTime).TotalMinutes;

            // Lägg till marginal så linjer inte går till kanten
            var x1 = (float)(area.Left + (minutes1FromStart / timeSpan) * (area.Width * 0.95));
            var x2 = (float)(area.Left + (minutes2FromStart / timeSpan) * (area.Width * 0.95));

            var y1 = CalculateYPosition(temp1, minTemp, tempRange, area);
            var y2 = CalculateYPosition(temp2, minTemp, tempRange, area);

            canvas.StrokeColor = Color.FromArgb("#000000");
            canvas.DrawLine(x1, y1, x2, y2);
        }

        // Rita datumpunkter som cirklar
        canvas.FillColor = Color.FromArgb("#007acc");
        foreach (var point in displayData)
        {
            var minutesFromStart = (point.Timestamp - firstPointTime).TotalMinutes;
            var x = (float)(area.Left + (minutesFromStart / timeSpan) * (area.Width * 0.95));
            var y = CalculateYPosition(point.Temperature, minTemp, tempRange, area);

            canvas.FillCircle(x, y, 3);
        }
    }

    private float CalculateYPosition(double temperature, double minTemp, double tempRange, RectF area)
    {
        var clampedTemp = Math.Max(minTemp, Math.Min(minTemp + tempRange, temperature));
        var normalizedPosition = (clampedTemp - minTemp) / tempRange;
        var y = area.Bottom - (normalizedPosition * area.Height);
        return (float)Math.Max(area.Top, Math.Min(area.Bottom, y));
    }

    // Uppdaterad axlar med display data
    private void DrawAxes(ICanvas canvas, RectF area, double minTemp, double maxTemp, List<TemperaturePoint> displayData)
    {
        canvas.StrokeColor = Color.FromArgb("#666666");
        canvas.StrokeSize = 3;

        canvas.DrawLine(area.Left, area.Top, area.Left, area.Bottom);
        canvas.DrawLine(area.Left, area.Bottom, area.Right, area.Bottom);

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

        // X-axel labels baserat på display data
        canvas.FontColor = Colors.White;
        canvas.FontSize = 10;

        if (displayData.Count > 0)
        {
            var firstTime = displayData.First().Timestamp;
            var lastTime = displayData.Last().Timestamp;
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
    #endregion

    #region Helper Classes
    // Hjälpklass för att lagra temperatur med tid
    private class TemperaturePoint
    {
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }
    #endregion
}
