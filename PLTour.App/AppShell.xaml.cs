using PLTour.App.Pages;

namespace PLTour.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Application.Current.UserAppTheme = AppTheme.Light;

        // Đăng ký các trang không nằm trong TabBar
        Routing.RegisterRoute(nameof(TourDetailPage), typeof(TourDetailPage));
    }
}