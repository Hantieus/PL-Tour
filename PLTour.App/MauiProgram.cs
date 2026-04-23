using Microsoft.Extensions.Logging;
using PLTour.App.Pages;
using PLTour.App.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui; // 1. Thêm namespace mới này

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
                // SỬA DÒNG NÀY: 
                // true: Cho phép nhạc tiếp tục phát khi thoát app/tắt màn hình (Android)
                // false: Tắt nhạc khi thoát app
                .UseMauiCommunityToolkitMediaElement(true)
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

            // 1. Đăng ký Service
            builder.Services.AddSingleton<OfflineDatabase>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<ApiService>(); // Đảm bảo đã đăng ký ApiService

            // Đăng ký các Service xử lý logic (Singleton để dùng chung một instance duy nhất)
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<RouteTrackingService>();
            // LƯU Ý: Với MediaElement, bạn không cần dòng builder.Services.AddSingleton(AudioManager.Current) 
            // vì MediaElement là một Control trên giao diện XAML, nó tự quản lý trình phát.

            // 2. Đăng ký các Pages
            builder.Services.AddTransient<LoadingPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<TourDetailPage>();

            return builder.Build();
        }
    }
}