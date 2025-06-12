using DistilleryMonitor.Mobile.Views;

namespace DistilleryMonitor.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrera routing för DetailPage
            Routing.RegisterRoute("detail", typeof(TemperatureDetailPage));
            Routing.RegisterRoute("sensor-settings", typeof(Views.SensorSettingsPage));
        }
    }
}
