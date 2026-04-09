using PLTour.App.Services;

namespace PLTour.App
{
    public partial class App : Application
    {
        // 1. Khai báo tham số LocationService để hệ thống tự động truyền (Inject) vào
        public App(LocationService locationService)
        {
            InitializeComponent();

            // Gán LoadingPage làm trang khởi đầu và truyền service vào
            MainPage = new Pages.LoadingPage(locationService);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 2. Trả về Window chứa MainPage hiện tại (tức là LoadingPage). 
            // Sau khi LoadingPage xử lý xong GPS, nó sẽ tự gọi Application.Current.MainPage = new AppShell();
            return new Window(MainPage);
        }
    }
}