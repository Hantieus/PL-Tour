using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;

namespace PLTour.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    // 1. Khai báo biến LocationService
    private readonly LocationService _locationService;

    // 2. Truyền LocationService qua Constructor (DI sẽ tự động làm việc này)
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

            // 1. Lấy danh sách Tour từ API
            var tours = await _apiService.GetMockToursAsync();

            // 2. Lấy vị trí người dùng từ "Kho lưu trữ" LocationService
            var userLoc = _locationService.GetSavedLocation();

            // 3. Tính toán khoảng cách cho từng tour
            foreach (var tour in tours)
            {
                // Nếu lấy được vị trí (khác null), tiến hành tính khoảng cách
                if (userLoc != null)
                {
                    Location tourLoc = new Location(tour.Latitude, tour.Longitude);
                    double distance = Location.CalculateDistance(userLoc, tourLoc, DistanceUnits.Kilometers);
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
        catch (Exception)
        {
            await DisplayAlert("Lỗi", "Không thể tải dữ liệu tour.", "OK");
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