using Microsoft.Extensions.Logging;
using PLTour.App.Pages;
using PLTour.App.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Plugin.Maui.Audio; // Thư viện âm thanh bạn vừa cài

namespace PLTour.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ==========================================
            // ĐĂNG KÝ DEPENDENCY INJECTION (DI)
            // ==========================================

            // 1. Đăng ký Service (Dùng chung 1 bản sao duy nhất cho toàn bộ App)
            builder.Services.AddSingleton<LocationService>();

            // ĐĂNG KÝ TRÌNH QUẢN LÝ ÂM THANH (Dòng quan trọng nhất)
            builder.Services.AddSingleton(AudioManager.Current);

            // 2. Đăng ký các Pages
            builder.Services.AddTransient<LoadingPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MapPage>();

            return builder.Build();
        }
    }
}