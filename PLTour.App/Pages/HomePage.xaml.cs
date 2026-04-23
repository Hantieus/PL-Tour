using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;

namespace PLTour.App.Pages;

public partial class HomePage : ContentPage
{
    // SỬA LỖI: Không khởi tạo bằng 'new', để Dependency Injection tự truyền vào
    private readonly ApiService _apiService;
    private readonly LocationService _locationService;

    // Cập nhật Constructor để nhận cả 2 Service từ hệ thống
    public HomePage(LocationService locationService, ApiService apiService)
    {
        InitializeComponent();
        _locationService = locationService;
        _apiService = apiService;
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

            // Lấy danh sách Tour từ API
            var tours = await _apiService.GetToursAsync();

            // Lấy vị trí người dùng đã lưu từ LocationService
            var userLoc = _locationService.GetSavedLocation();

            if (tours != null)
            {
                foreach (var tour in tours)
                {
                    if (userLoc != null)
                    {
                        var tourLoc = new Microsoft.Maui.Devices.Sensors.Location(tour.Latitude, tour.Longitude);
                        double distance = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(userLoc, tourLoc, DistanceUnits.Kilometers);
                        tour.DistanceDisplay = $"Cách bạn: {distance:F1} km";
                    }
                    else
                    {
                        tour.DistanceDisplay = "Vị trí chưa xác định";
                    }
                }

                TourListView.ItemsSource = tours;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi LoadTours: {ex.Message}");
            // SỬA LỖI: Dùng DisplayAlertAsync thay cho DisplayAlert (obsolete)
            await DisplayAlertAsync("Lỗi", "Không thể kết nối đến máy chủ để tải dữ liệu tour.", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var currentPage = Application.Current?.Windows[0]?.Page;
            if (currentPage != null)
            {
                await currentPage.DisplayAlert(title, message, cancel);
            }
        });
    }

    private async void IntroApp_Clicked(object sender, EventArgs e)
    {
        await TextToSpeech.SpeakAsync("Chào mừng bạn đến với PL Tour. Ứng dụng đồng hành đáng tin cậy của bạn.");
    }

    private async void GoToMap_Clicked(object sender, EventArgs e)
    {
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

        await Shell.Current.GoToAsync(nameof(TourDetailPage), navigationParameter);
    }
}