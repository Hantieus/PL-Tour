using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using PLTour.App.Pages;
using PLTour.App.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PLTour.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkitMediaElement(true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<DeviceMonitorService>();
        builder.Services.AddSingleton<IAudioService, AudioService>();

        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<TourDetailPage>();

        return builder.Build();
    }
}
