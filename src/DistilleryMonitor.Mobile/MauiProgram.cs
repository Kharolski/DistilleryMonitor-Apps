using DistilleryMonitor.Core.Services;
using DistilleryMonitor.Mobile.Services;

namespace DistilleryMonitor.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrera våra services för Dependency Injection
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<ApiService>();

            // Registrera sidor (kommer senare)
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
