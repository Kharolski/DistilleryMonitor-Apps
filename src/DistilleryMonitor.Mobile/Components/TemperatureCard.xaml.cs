using DistilleryMonitor.Core.Models;

namespace DistilleryMonitor.Mobile.Components;

public partial class TemperatureCard : ContentView
{
    public TemperatureCard()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty SensorProperty =
        BindableProperty.Create(nameof(Sensor), typeof(TemperatureReading), typeof(TemperatureCard),
            propertyChanged: OnSensorChanged);

    public TemperatureReading Sensor
    {
        get => (TemperatureReading)GetValue(SensorProperty);
        set => SetValue(SensorProperty, value);
    }

    // Callback när Sensor-property ändras
    private static void OnSensorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureCard card && newValue is TemperatureReading sensor)
        {
            // Debug - vad får vi för data?
            System.Diagnostics.Debug.WriteLine($"TemperatureCard: Setting sensor {sensor.Name} (ID: {sensor.Id}, Temp: {sensor.Temperature})");
            card.BindingContext = sensor; // ← BindingContext till sensor-objektet
        }
    }

    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (Sensor != null)
        {
            System.Diagnostics.Debug.WriteLine($"Card tapped: {Sensor.Name} (ID: {Sensor.Id})");
            await Shell.Current.GoToAsync($"detail?sensorId={Sensor.Id}");
        }
    }
}
