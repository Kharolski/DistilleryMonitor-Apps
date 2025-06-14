using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();

            // STARTA SENSOR MONITORING
            StartSensorMonitoring();
        }

        private async void StartSensorMonitoring()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 Försöker starta sensor monitoring...");

                var notificationService = Handler?.MauiContext?.Services?.GetService<IAppNotificationService>();

                if (notificationService != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ NotificationService hittad!");
                    notificationService.StartSensorMonitoring(5);
                    System.Diagnostics.Debug.WriteLine("🔍 Sensor monitoring startad från App.xaml.cs");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ NotificationService är NULL!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Kunde inte starta sensor monitoring: {ex.Message}");
            }
        }
    }
}
