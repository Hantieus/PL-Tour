using Microsoft.Extensions.Logging;
using PLTourApp.Database;
using PLTourApp.Engines;
using PLTourApp.Services;
using PLTourApp.ViewModels;

namespace PLTourApp;

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

        // ===== DATABASE =====
        builder.Services.AddSingleton<SQLiteHelper>();

        // ===== SERVICES =====
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<TTSService>();
        builder.Services.AddSingleton<NarrationEngine>();
        builder.Services.AddSingleton<LocationService>();

        // ===== VIEWMODELS =====
        builder.Services.AddSingleton<MapViewModel>();

        // ===== PAGES (khuyên dùng) =====
        builder.Services.AddSingleton<Views.HomePage>();
        builder.Services.AddTransient<Views.MapPage>();
        builder.Services.AddTransient<Views.PoiListPage>();
        builder.Services.AddTransient<Views.QRScannerPage>();
        builder.Services.AddTransient<Views.AnalyticsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}