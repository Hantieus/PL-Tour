using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Media;

namespace PLTour.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    private readonly LocationService _locationService;
    private readonly DeviceMonitorService _deviceMonitorService;
    private readonly IAudioService _audioService;

    private string _syncStatusText = string.Empty;
    public string SyncStatusText
    {
        get => _syncStatusText;
        set { _syncStatusText = value; OnPropertyChanged(nameof(SyncStatusText)); }
    }

    // 2. Truyền LocationService qua Constructor
    public HomePage(LocationService locationService, DeviceMonitorService deviceMonitorService, IAudioService audioService)
    {
        InitializeComponent();
        _locationService = locationService;
        _deviceMonitorService = deviceMonitorService;
        _audioService = audioService;
        _audioService.PlaybackStopped += AudioService_PlaybackStopped;
        LocalizationService.Instance.LanguageChanged += (_, __) => OnPropertyChanged(string.Empty);
        Connectivity.Current.ConnectivityChanged += Connectivity_Changed;
        BindingContext = this;
    }

    private void AudioService_PlaybackStopped(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_audioService.IsPlaying)
            {
                var tours = TourListView.ItemsSource as IEnumerable<TourModel>;
                if (tours != null)
                {
                    foreach (var t in tours)
                    {
                        t.IsPlaying = false;
                    }
                }
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[HOME] OnAppearing start");
        await _deviceMonitorService.TrackEventAsync("screen_view", new PLTour.Shared.Models.DTO.AnalyticsEventDto { Keyword = "home" });
        UpdateOfflineBanner();
        UpdateSyncStatus();
        await LoadToursAsync();
        System.Diagnostics.Debug.WriteLine("[HOME] OnAppearing end");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Connectivity.Current.ConnectivityChanged -= Connectivity_Changed;
    }

    private async Task LoadToursAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;

            var connectionOk = await _apiService.TestConnectionAsync();
            UpdateSyncStatus();
            System.Diagnostics.Debug.WriteLine($"[HOME] Connection test = {connectionOk}");
            OfflineBanner.IsVisible = !connectionOk;
            if (!connectionOk)
            {
                System.Diagnostics.Debug.WriteLine("[HOME] Connection test failed, but continue loading tours anyway.");
            }

            var tours = await _apiService.GetToursAsync();
            if (tours != null)
            {
                await OfflineCacheService.Instance.SaveToursAsync(tours);
            }
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
                        tour.DistanceDisplay = string.Format(LocalizationService.Instance["DistanceFromYou"], distance.ToString("F1"));
                    }
                    else
                    {
                        tour.DistanceDisplay = LocalizationService.Instance["LocationUnknown"];
                    }

                    tour.DistanceDisplay = tour.DistanceDisplay;
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
            await this.DisplayAlertAsync(LocalizationService.Instance["Error"], LocalizationService.Instance["CannotLoadTourData"], LocalizationService.Instance["OK"]);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }


    private async void GoToMap_Clicked(object sender, EventArgs e)
    {
        // Chế độ xem tự do: vào map không truyền tour cụ thể
        await Shell.Current.GoToAsync("//map");
    }

    private async void SpeakTour_Clicked(object sender, EventArgs e)
    {
        var tour = (sender as Button)?.CommandParameter as TourModel;
        if (tour == null) return;

        if (tour.IsPlaying)
        {
            await _audioService.StopAsync();
            return;
        }

        // Dừng cái cũ
        await _audioService.StopAsync();

        // Kiểm tra nội dung
        if (string.IsNullOrWhiteSpace(tour.LocalizedIntroText) && string.IsNullOrEmpty(tour.LocalizedIntroAudioUrl))
        {
            await DisplayAlert(LocalizationService.Instance["Information"], LocalizationService.Instance["NoNarrationYet"], LocalizationService.Instance["OK"]);
            return;
        }

        // Cập nhật trạng thái
        var tours = TourListView.ItemsSource as IEnumerable<TourModel>;
        if (tours != null)
        {
            foreach (var t in tours) t.IsPlaying = (t == tour);
        }

        try
        {
            string audioUrl = FixAudioUrl(tour.LocalizedIntroAudioUrl);
            if (!string.IsNullOrEmpty(audioUrl))
            {
                await _audioService.PlayAudioAsync(audioUrl);
            }
            else if (!string.IsNullOrWhiteSpace(tour.LocalizedIntroText))
            {
                string langCode = Preferences.Default.Get("UserLanguage", "vi");
                await _audioService.PlayTextToSpeechAsync(tour.LocalizedIntroText, langCode);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HOME] Audio error: {ex.Message}");
            tour.IsPlaying = false;
        }
    }

    private string? FixAudioUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.Contains("localhost"))
        {
            url = url.Replace("localhost:7291", "q0x087zj-5229.asse.devtunnels.ms");
            url = url.Replace("http://", "https://");
        }
        return url;
    }

    private void UpdateOfflineBanner()
    {
        OfflineBanner.IsVisible = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
    }

    private void UpdateSyncStatus()
    {
        var isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        var pending = DeviceMonitorService.Instance?.PendingCount ?? 0;
        SyncStatusText = isOnline
            ? (pending > 0 ? $"Đang đồng bộ {pending} sự kiện..." : "Đã kết nối máy chủ")
            : pending > 0 ? $"Offline — còn {pending} sự kiện chờ gửi" : "Offline — dữ liệu sẽ được gửi sau";
    }

    private void Connectivity_Changed(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner();
            UpdateSyncStatus();
        });
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