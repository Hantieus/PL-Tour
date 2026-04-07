using PLTour.App.Services;

namespace PLTour.App.Pages;

public partial class LoadingPage : ContentPage
{
    // Chỉ khai báo biến, không khởi tạo bằng 'new'
    private readonly LocationService _locationService;

    // MAUI DI Container sẽ tự động truyền instance duy nhất của LocationService vào đây
    public LoadingPage(LocationService locationService)
    {
        InitializeComponent();
        _locationService = locationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
        try
        {
            lblStatus.Text = "Đang xác định vị trí...";

            // Gọi hàm từ Singleton Service đã được Inject
            var location = await _locationService.GetAndSaveCurrentLocationAsync();

            if (location == null)
            {
                lblStatus.Text = "Không thể lấy vị trí, dùng mặc định.";
                await Task.Delay(1500); // Cho người dùng kịp đọc thông báo
            }
        }
        catch (Exception)
        {
            // Có thể log lỗi ra đây nếu cần thiết
            lblStatus.Text = "Lỗi khởi tạo hệ thống.";
            await Task.Delay(1000);
        }

        // Chuyển trang an toàn trên luồng chính (Main UI Thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new AppShell();
        });
    }
}