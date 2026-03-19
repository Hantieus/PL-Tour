using Mapsui.UI.Maui.Extensions; // <--- Namespace chứa .UseMapsui()
using Microsoft.Extensions.Logging;
using PLTourApp.Database;
using PLTourApp.Engines;
using PLTourApp.Services;
using PLTourApp.ViewModels;
using SkiaSharp.Views.Maui.Controls.Hosting; // <--- Namespace chứa .UseSkiaSharp()

namespace PLTourApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()   
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp() // Cần thiết để Mapsui vẽ được trên MAUI
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // ===== DATABASE =====
        builder.Services.AddSingleton<SQLiteHelper>();

        // ===== SERVICES =====
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<TTSService>();
        builder.Services.AddSingleton<LocationService>();

        // ===== ENGINES =====
        builder.Services.AddSingleton<NarrationEngine>();
        builder.Services.AddSingleton<GeofenceEngine>();
        builder.Services.AddSingleton<LocationManager>();

        // ===== VIEWMODELS =====
        builder.Services.AddSingleton<MapViewModel>();

        // ===== PAGES =====
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