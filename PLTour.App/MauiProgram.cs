using Microsoft.Extensions.Logging;
using PLTour.App.Pages;
// Thêm thư mục chứa Service và Pages của bạn vào đây (nếu có)
using PLTour.App.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;
// using PLTour.App.Pages; // Bỏ comment dòng này nếu các trang của bạn nằm trong thư mục Pages

namespace PLTour.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp() // Xóa bỏ tham số (true) ở đây
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

            // 2. Đăng ký các Pages (Mỗi lần mở trang sẽ tạo một bản sao mới)
            // Lưu ý: Đổi tên LoadingPage, HomePage, MapPage cho đúng với tên class thực tế của bạn
            builder.Services.AddTransient<LoadingPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MapPage>();

            return builder.Build();
        }
    }
}