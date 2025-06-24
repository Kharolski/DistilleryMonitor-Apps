using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Mobile.Services;
using DistilleryMonitor.Mobile.ViewModels;
using DistilleryMonitor.Mobile.Views;
using Plugin.LocalNotification;

namespace DistilleryMonitor.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrera services för Dependency Injection
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<IAppNotificationService, AppNotificationService>();

            // Developer Service
            builder.Services.AddSingleton<IDeveloperService, DeveloperService>();

            // Temperature Threshold Service
            builder.Services.AddSingleton<TemperatureThresholdService>();

            // BEHÅLL TILLFÄLLIGT: MockDataService med ISettingsService
            builder.Services.AddSingleton<MockDataService>(provider =>
                new MockDataService(provider.GetService<ISettingsService>()));

            // Registrera ViewModels
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<TemperatureDetailViewModel>();
            builder.Services.AddTransient<SensorSettingsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AboutPageViewModel>();

            // Registrera sidor 
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<TemperatureDetailPage>();
            builder.Services.AddTransient<SensorSettingsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AboutPage>();

#if ANDROID
            // Registrera custom Shell renderer
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<Shell, DistilleryMonitor.Mobile.Platforms.Android.CustomShellRenderer>();
            });
#endif

            return builder.Build();
        }
    }
}
