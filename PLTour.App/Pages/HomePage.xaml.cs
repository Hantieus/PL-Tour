using Microsoft.Maui;
using Microsoft.Maui.Controls;
using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Media;

namespace PLTour.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    // 1. Khai báo biến LocationService
    private readonly LocationService _locationService;
    private readonly DeviceMonitorService _deviceMonitorService;

    // 2. Truyền LocationService qua Constructor
    public HomePage(LocationService locationService, DeviceMonitorService deviceMonitorService)
    {
        InitializeComponent();
        _locationService = locationService;
        _deviceMonitorService = deviceMonitorService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[HOME] OnAppearing start");
        await _deviceMonitorService.TrackEventAsync("screen_view", new PLTour.Shared.Models.DTO.AnalyticsEventDto { Keyword = "home" });
        await LoadToursAsync();
        System.Diagnostics.Debug.WriteLine("[HOME] OnAppearing end");
    }

    private async Task LoadToursAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;

            var connectionOk = await _apiService.TestConnectionAsync();
            System.Diagnostics.Debug.WriteLine($"[HOME] Connection test = {connectionOk}");
            if (!connectionOk)
            {
                System.Diagnostics.Debug.WriteLine("[HOME] Connection test failed, but continue loading tours anyway.");
            }

            var tours = await _apiService.GetToursAsync();
            System.Diagnostics.Debug.WriteLine($"[HOME] Loaded tours: {tours?.Count ?? 0}");

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

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TourListView.ItemsSource = null;
                    TourListView.ItemsSource = tours;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HOME_ERROR] LoadTours: {ex}");
            await this.DisplayAlertAsync("Lỗi", "Không thể kết nối đến máy chủ để tải dữ liệu tour.", "OK");
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
        // Chế độ xem tự do: vào map không truyền tour cụ thể
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