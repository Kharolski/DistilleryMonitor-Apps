using Microsoft.Maui.Graphics;
using DistilleryMonitor.Core.Models;

namespace DistilleryMonitor.Mobile.Components;

public class CombinedTemperatureGraphView : GraphicsView, IDrawable
{
    #region Bindable Properties
    /// <summary>
    /// BindableProperty för att ta emot temperatur data
    /// </summary>
    public static readonly BindableProperty TemperatureDataProperty =
        BindableProperty.Create(nameof(TemperatureData), typeof(List<TemperatureHistory>),
            typeof(CombinedTemperatureGraphView), new List<TemperatureHistory>(),
            propertyChanged: OnTemperatureDataChanged);

    public List<TemperatureHistory> TemperatureData
    {
        get => (List<TemperatureHistory>)GetValue(TemperatureDataProperty);
        set => SetValue(TemperatureDataProperty, value);
    }
    #endregion

    #region Constructor
    public CombinedTemperatureGraphView()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;
    }
    #endregion

    #region Property Changed Events
    private static void OnTemperatureDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CombinedTemperatureGraphView view)
        {
            view.Invalidate(); // Rita om grafen när data ändras
        }
    }
    #endregion

    #region Drawing Methods
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Kontrollera om vi har data
        if (TemperatureData == null || TemperatureData.Count == 0)
        {
            DrawNoDataMessage(canvas, dirtyRect);
            return;
        }

        // Mer plats för labels och marginal höger
        var graphArea = new RectF(
            dirtyRect.Left + 40,    // Mer plats för Y-axel labels
            dirtyRect.Top + 15,     // Plats för titel
            dirtyRect.Width - 60,   // Plats till höger
            dirtyRect.Height - 45   // Plats för X-axel labels
        );

        // Beräkna gemensam skalning för alla sensorer
        var allTemps = TemperatureData.Select(d => d.Temperature).ToList();
        var minTemp = Math.Floor(allTemps.Min() / 5) * 5 - 5;  // Runda till närmaste 5-tal
        var maxTemp = Math.Ceiling(allTemps.Max() / 5) * 5 + 5;
        var minTime = TemperatureData.Min(d => d.Timestamp);
        var maxTime = TemperatureData.Max(d => d.Timestamp);

        // Rita i rätt ordning
        DrawGraphBackground(canvas, graphArea);
        DrawGrid(canvas, graphArea, minTemp, maxTemp, minTime, maxTime);  // Rutnät först
        DrawAxes(canvas, graphArea);                                      // Axlar över rutnät
        DrawAxisLabels(canvas, graphArea, minTemp, maxTemp, minTime, maxTime); // Labels

        // Rita temperaturlinjer sist (över allt annat)
        DrawTemperatureLineWithScale(canvas, graphArea, "Kolv", Color.FromArgb("#007acc"), minTemp, maxTemp, minTime, maxTime);
        DrawTemperatureLineWithScale(canvas, graphArea, "Destillat", Color.FromArgb("#28a745"), minTemp, maxTemp, minTime, maxTime);
        DrawTemperatureLineWithScale(canvas, graphArea, "Kylare", Color.FromArgb("#dc3545"), minTemp, maxTemp, minTime, maxTime);
    }

    private void DrawGraphBackground(ICanvas canvas, RectF area)
    {
        // Rita ljus bakgrund för grafen
        canvas.FillColor = Color.FromArgb("#f8f9fa");
        canvas.FillRoundedRectangle(area, 8);

        // Rita kant
        canvas.StrokeColor = Color.FromArgb("#dee2e6");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(area, 8);
    }

    // Rutnät
    private void DrawGrid(ICanvas canvas, RectF area, double minTemp, double maxTemp, DateTime minTime, DateTime maxTime)
    {
        canvas.StrokeColor = Color.FromArgb("#a8abad"); // rutnät
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = new float[] { 2, 2 }; // Streckad linje

        // Vertikala linjer (tid)
        var timeSteps = 6; // 6 vertikala linjer

        for (int i = 1; i < timeSteps; i++)
        {
            var x = area.Left + (area.Width * i / timeSteps);
            canvas.DrawLine(x, area.Top, x, area.Bottom);
        }

        // Horisontella linjer (temperatur)
        var tempSteps = 6; // 6 horisontella linjer

        for (int i = 1; i < tempSteps; i++)
        {
            var y = area.Bottom - (area.Height * i / tempSteps);
            canvas.DrawLine(area.Left, y, area.Right, y);
        }

        canvas.StrokeDashPattern = null; // Återställ till solid linje
    }

    // Rita axlar
    private void DrawAxes(ICanvas canvas, RectF area)
    {
        canvas.StrokeColor = Color.FromArgb("#495057"); // Mörkgrå axlar
        canvas.StrokeSize = 2;

        // Y-axel (vänster)
        canvas.DrawLine(area.Left, area.Top, area.Left, area.Bottom);

        // X-axel (botten)
        canvas.DrawLine(area.Left, area.Bottom, area.Right, area.Bottom);

        // Höger Y-axel (för att rama in)
        canvas.DrawLine(area.Right, area.Top, area.Right, area.Bottom);

        // Topp X-axel (för att rama in)
        canvas.DrawLine(area.Left, area.Top, area.Right, area.Top);
    }

    // Fixad label-method med ljusa färger
    private void DrawAxisLabels(ICanvas canvas, RectF area, double minTemp, double maxTemp, DateTime minTime, DateTime maxTime)
    {
        canvas.FontColor = Colors.White; // Vit text för mörk bakgrund
        canvas.FontSize = 10;

        // Y-axel labels (temperatur)
        var tempRange = maxTemp - minTemp;
        var tempSteps = 6;

        for (int i = 0; i <= tempSteps; i++)
        {
            var temp = minTemp + (tempRange * i / tempSteps);
            var y = area.Bottom - (area.Height * i / tempSteps);

            canvas.DrawString($"{temp:F0}°C", area.Left - 10, y, HorizontalAlignment.Right);
        }

        // X-axel labels (tid)
        var timeSpan = maxTime - minTime;
        var timeSteps = 5;

        for (int i = 0; i <= timeSteps; i++)
        {
            var time = minTime.AddMinutes(timeSpan.TotalMinutes * i / timeSteps);
            var x = area.Left + (area.Width * i / timeSteps);

            canvas.DrawString(time.ToString("HH:mm"), x, area.Bottom + 20, HorizontalAlignment.Center);
        }
    }

    // Fixad linje-ritning med marginal
    private void DrawTemperatureLineWithScale(ICanvas canvas, RectF area, string sensorName, Color lineColor,
        double minTemp, double maxTemp, DateTime minTime, DateTime maxTime)
    {
        var sensorData = TemperatureData
            .Where(d => d.SensorName == sensorName)
            .OrderBy(d => d.Timestamp)
            .ToList();

        if (sensorData.Count < 2)
            return;

        var timeSpan = maxTime - minTime;
        if (timeSpan.TotalMinutes < 1)
            return;

        canvas.StrokeColor = lineColor;
        canvas.StrokeSize = 3; // Tjockare linjer
        canvas.StrokeDashPattern = null; // Solid linje

        PathF path = new PathF();
        bool firstPoint = true;

        foreach (var point in sensorData)
        {
            // Lägg till marginal så linjer inte går till kanten
            var timeProgress = (point.Timestamp - minTime).TotalMinutes / timeSpan.TotalMinutes;
            var x = area.Left + (float)(timeProgress * (area.Width * 0.95)); // 95% av bredden
            var y = area.Bottom - (float)((point.Temperature - minTemp) / (maxTemp - minTemp) * area.Height);

            if (firstPoint)
            {
                path.MoveTo(x, y);
                firstPoint = false;
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        canvas.DrawPath(path);

        canvas.FillColor = lineColor;
        foreach (var point in sensorData)
        {
            var timeProgress = (point.Timestamp - minTime).TotalMinutes / timeSpan.TotalMinutes;
            var x = area.Left + (float)(timeProgress * (area.Width * 0.95)); // Samma marginal
            var y = area.Bottom - (float)((point.Temperature - minTemp) / (maxTemp - minTemp) * area.Height);

            canvas.FillCircle(x, y, 3); // Små cirklar på datapunkterna
        }
    }

    // "Inga Data" meddelande
    private void DrawNoDataMessage(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontSize = 14;
        canvas.FontColor = Colors.White; // Vit text för mörk bakgrund
        canvas.DrawString("📈 Ingen historisk data",
            dirtyRect.Center.X,
            dirtyRect.Center.Y,
            HorizontalAlignment.Center);
    }

    #endregion
}
