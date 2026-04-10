using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;

namespace PLTour.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    // 1. Khai báo biến LocationService
    private readonly LocationService _locationService;

    // 2. Truyền LocationService qua Constructor
    public HomePage(LocationService locationService)
    {
        InitializeComponent();
        _locationService = locationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadToursAsync();
    }

    private async Task LoadToursAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;

            // --- SỬA TẠI ĐÂY: Lấy danh sách Tour từ API thật ---
            var tours = await _apiService.GetToursAsync();

            // 2. Lấy vị trí người dùng từ "Kho lưu trữ" LocationService
            var userLoc = _locationService.GetSavedLocation();

            // 3. Tính toán khoảng cách cho từng tour
            if (tours != null)
            {
                foreach (var tour in tours)
                {
                    // Nếu lấy được vị trí người dùng, tiến hành tính khoảng cách
                    if (userLoc != null)
                    {
                        // Sử dụng Microsoft.Maui.Devices.Sensors.Location để tính toán
                        var tourLoc = new Microsoft.Maui.Devices.Sensors.Location(tour.Latitude, tour.Longitude);
                        double distance = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(userLoc, tourLoc, DistanceUnits.Kilometers);

                        tour.DistanceDisplay = $"Cách bạn: {distance:F1} km";
                    }
                    else
                    {
                        tour.DistanceDisplay = "Vị trí chưa xác định";
                    }
                }

                // Cập nhật danh sách lên giao diện
                TourListView.ItemsSource = tours;
            }
        }
        catch (Exception ex)
        {
            // In ra debug để bạn dễ theo dõi nếu API lỗi
            System.Diagnostics.Debug.WriteLine($"Lỗi LoadTours: {ex.Message}");
            await DisplayAlert("Lỗi", "Không thể kết nối đến máy chủ để tải dữ liệu tour.", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void IntroApp_Clicked(object sender, EventArgs e)
    {
        await TextToSpeech.SpeakAsync("Chào mừng bạn đến với PL Tour. Ứng dụng đồng hành đáng tin cậy của bạn.");
    }

    private async void GoToMap_Clicked(object sender, EventArgs e)
    {
        // Điều hướng sang Tab Map
        await Shell.Current.GoToAsync("//map");
    }

    private async void SpeakTour_Clicked(object sender, EventArgs e)
    {
        var text = (sender as Button)?.CommandParameter as string;
        if (!string.IsNullOrEmpty(text))
            await TextToSpeech.Default.SpeakAsync(text);
    }

    private async void ViewTourDetail_Clicked(object sender, EventArgs e)
    {
        // Lưu ý: Đảm bảo class trong Models tên là TourModel hoặc Tour tùy theo Project của bạn
        var tour = (sender as Button)?.CommandParameter as TourModel;
        if (tour == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "SelectedTour", tour }
        };

        // Điều hướng sang trang Chi tiết Tour
        await Shell.Current.GoToAsync(nameof(TourDetailPage), navigationParameter);
    }
}