using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using MauiColor = Microsoft.Maui.Graphics.Color;
using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Threading;

namespace PLTour.App.Pages;

[QueryProperty(nameof(Tour), nameof(Tour))]
[QueryProperty(nameof(Tour), "SelectedTour")]
public partial class TourDetailPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly IAudioService _audioService;
    private TourModel? _tour;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private Microsoft.Maui.Devices.Sensors.Location? _lastDistanceUpdateLocation;

    public TourModel? Tour
    {
        get => _tour;
        set
        {
            _tour = value;
            OnPropertyChanged(nameof(Tour));
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[TOUR] Tour assigned: {(_tour != null ? _tour.Name : "null")}, POIs={_tour?.Pois?.Count ?? 0}");
                LoadTourData();
            });
        }
    }

    public ObservableCollection<PoiModel> PoiListSource { get; } = new();

    public TourDetailPage(LocationService locationService, IAudioService audioService)
    {
        InitializeComponent();
        BindingContext = this;
        _locationService = locationService;
        _audioService = audioService;
        _audioService.PlaybackStopped += AudioService_PlaybackStopped;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        PoiList.ItemsSource = PoiListSource;
        SetActiveFilter(PoiCategories.ThamQuan);
    }

    private void AudioService_PlaybackStopped(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_tour != null) _tour.IsPlaying = false;

            foreach (var poi in PoiListSource)
            {
                poi.IsPlaying = false;
            }

            _ = AnalyticsService.Instance.TrackAudioStopAsync();
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Không đăng ký lại event ở đây vì đã đăng ký trong constructor
        LoadTourData();
        StartTracking();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTracking();
    }

    private void LoadTourData()
    {
        if (_tour == null) return;

        lblTourName.Text = _tour.Name;
        imgTour.Source = _tour.ImageUrl;

        PoiListSource.Clear();
        foreach (var poi in _tour.Pois ?? [])
        {
            poi.IsPlaying = false;
            PoiListSource.Add(poi);
        }

        ApplyCurrentFilter();
        UpdateDistances();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetActiveFilter(GetActiveCategory());
            if (_tour != null)
                lblTourName.Text = _tour.Name;
        });
    }

    private void StartTracking()
    {
        if (_trackingTask is { IsCompleted: false }) return;
        _trackingCts = new CancellationTokenSource();
        _trackingTask = TrackLoopAsync(_trackingCts.Token);
    }

    private void StopTracking()
    {
        if (_trackingCts == null) return;
        _trackingCts.Cancel();
        _trackingCts.Dispose();
        _trackingCts = null;
    }

    private async Task TrackLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await _locationService.GetAndSaveCurrentLocationAsync();
                if (location != null && ShouldUpdateDistanceUI(location))
                {
                    UpdateDistances();
                    _lastDistanceUpdateLocation = location;
                }
            }
            catch
            {
                // Bỏ qua lỗi định vị tạm thời để tránh crash UI
            }

            try
            {
                await Task.Delay(5000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void UpdateDistances()
    {
        var userLoc = _locationService.CurrentLocation;
        if (userLoc == null || _tour?.Pois == null) return;

        foreach (var poi in _tour.Pois)
        {
            var distMeters = CalculateDistance(userLoc.Latitude, userLoc.Longitude, poi.Lat, poi.Lng);
            poi.DistanceMeters = distMeters;
        }

        ApplyCurrentFilter();
    }

    private bool ShouldUpdateDistanceUI(Microsoft.Maui.Devices.Sensors.Location currentLocation)
    {
        if (_lastDistanceUpdateLocation == null) return true;
        var movedMeters = CalculateDistance(
            currentLocation.Latitude,
            currentLocation.Longitude,
            _lastDistanceUpdateLocation.Latitude,
            _lastDistanceUpdateLocation.Longitude);
        return movedMeters > 8;
    }

    private void ApplyCurrentFilter()
    {
        if (_tour?.Pois == null) return;

        var activeCategory = CanonicalizeCategory(GetActiveCategory());
        var filtered = _tour.Pois
            .Where(p => CanonicalizeCategory(p.Category, p.CategoryId) == activeCategory)
            .OrderBy(p => p.DistanceMeters)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PoiListSource.Clear();
            foreach (var poi in filtered)
                PoiListSource.Add(poi);

            System.Diagnostics.Debug.WriteLine($"[TOUR] Filter '{activeCategory}' => {filtered.Count} POIs");
        });
    }

    private string GetActiveCategory()
    {
        if (BtnFoodPlaces.BackgroundColor == MauiColor.FromArgb("#2A9D8F")) return PoiCategories.AnUong;
        if (BtnEvents.BackgroundColor == MauiColor.FromArgb("#2A9D8F")) return PoiCategories.SuKien;
        return PoiCategories.ThamQuan;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371000d;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private void PoiItem_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not PoiModel poi) return;
        this.PoiDetailPopupView.ShowPopup(poi);
        _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);
    }

    private async void SpeakIntro_Clicked(object? sender, EventArgs e)
    {
        if (_tour == null) return;

        var intro = _tour.IntroText?.Trim();
        if (string.IsNullOrWhiteSpace(intro)) return;

        if (_tour.IsPlaying)
        {
            await _audioService.StopAsync();
            return;
        }

        // Dừng tất cả đang phát trước khi bắt đầu cái mới
        await _audioService.StopAsync();

        // Cập nhật trạng thái sau khi đã dừng
        _tour.IsPlaying = true;
        foreach (var p in PoiListSource) p.IsPlaying = false;

        try
        {
            string langCode = Preferences.Default.Get("UserLanguage", "vi");
            await AnalyticsService.Instance.TrackAudioStartAsync(_tour.Id.GetHashCode(), langCode, _locationService.CurrentLocation != null);

            string url = FixAudioUrl(_tour.IntroAudioUrl);
            if (!string.IsNullOrEmpty(url))
            {
                await _audioService.PlayAudioAsync(url);
            }
            else
            {
                await _audioService.PlayTextToSpeechAsync(intro, langCode);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TOUR] Audio intro error: {ex.Message}");
            _tour.IsPlaying = false;
        }
    }

    private async void ViewMap_Clicked(object? sender, EventArgs e)
    {
        if (_tour == null) return;

        var route = $"//map?TourId={Uri.EscapeDataString(_tour.Id)}";
        System.Diagnostics.Debug.WriteLine($"[NAV] TourDetail -> Map: {route}");
        await Shell.Current.GoToAsync(route);
    }

    private async void BtnRoute_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not PoiModel poi) return;
        await Microsoft.Maui.ApplicationModel.Map.OpenAsync(
            new Microsoft.Maui.Devices.Sensors.Location(poi.Lat, poi.Lng),
            new MapLaunchOptions { Name = poi.Name, NavigationMode = NavigationMode.Driving });
    }

    private async void BtnSpeakPoi_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not PoiModel poi) return;
        await SpeakPoiAsync(poi);
    }

    private void FilterTouristSpots_Clicked(object? sender, EventArgs e) => SetActiveFilter(PoiCategories.ThamQuan);
    private void FilterFoodPlaces_Clicked(object? sender, EventArgs e) => SetActiveFilter(PoiCategories.AnUong);
    private void FilterEvents_Clicked(object? sender, EventArgs e) => SetActiveFilter(PoiCategories.SuKien);

    private void SetActiveFilter(string category)
    {
        BtnTouristSpots.BackgroundColor = category == PoiCategories.ThamQuan ? MauiColor.FromArgb("#2A9D8F") : MauiColor.FromArgb("#E9ECEF");
        BtnTouristSpots.TextColor = category == PoiCategories.ThamQuan ? Colors.White : Colors.Black;

        BtnFoodPlaces.BackgroundColor = category == PoiCategories.AnUong ? MauiColor.FromArgb("#2A9D8F") : MauiColor.FromArgb("#E9ECEF");
        BtnFoodPlaces.TextColor = category == PoiCategories.AnUong ? Colors.White : Colors.Black;

        BtnEvents.BackgroundColor = category == PoiCategories.SuKien ? MauiColor.FromArgb("#2A9D8F") : MauiColor.FromArgb("#E9ECEF");
        BtnEvents.TextColor = category == PoiCategories.SuKien ? Colors.White : Colors.Black;

        ApplyCurrentFilter();
    }

    private static string CanonicalizeCategory(string? category, int? categoryId = null)
    {
        if (categoryId is >= 1 and <= 3)
            return categoryId.Value.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(category))
            return "1";

        var normalized = RemoveDiacritics(category)
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Trim();

        if (normalized.Contains("anuong") || normalized.Contains("food"))
            return "2";

        if (normalized.Contains("sukien") || normalized.Contains("event"))
            return "3";

        return "1";
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }



    private async void PoiDetailPopupView_SpeakRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;
        await SpeakPoiAsync(poi);
    }

    private async Task SpeakPoiAsync(PoiModel poi)
    {
        try
        {
            if (poi.IsPlaying)
            {
                await _audioService.StopAsync();
                return;
            }

            // Dừng tất cả trước
            await _audioService.StopAsync();

            // Cập nhật trạng thái
            if (_tour != null) _tour.IsPlaying = false;
            foreach (var item in PoiListSource) item.IsPlaying = (item == poi);

            var audioUrl = FixAudioUrl(poi.AudioUrl);
            if (!string.IsNullOrWhiteSpace(audioUrl))
            {
                await _audioService.PlayAudioAsync(audioUrl);
                return;
            }

            // Fallback: TTS
            var speakText = string.IsNullOrWhiteSpace(poi.FullContent) ? poi.Description : poi.FullContent;
            speakText = speakText?.Trim();
            if (string.IsNullOrWhiteSpace(speakText))
            {
                poi.IsPlaying = false;
                return;
            }

            string langCode = Preferences.Default.Get("UserLanguage", poi.LanguageCode ?? "vi");
            await _audioService.PlayTextToSpeechAsync($"{poi.Name}. {speakText}", langCode);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TOUR] Speak POI failed: {ex}");
            poi.IsPlaying = false;
        }
    }

    private string? FixAudioUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        // Xử lý localhost nếu có (thường xảy ra khi backend trả về link absolute với localhost)
        if (url.Contains("localhost"))
        {
            url = url.Replace("localhost:7291", "q0x087zj-7291.asse.devtunnels.ms");
            url = url.Replace("http://", "https://");
        }
        return url;
    }

    private async void PoiDetailPopupView_CloseRequested(object sender, EventArgs e)
    {
        await _audioService.StopAsync();
        this.PoiDetailPopupView.HidePopup();
    }

    private void PoiDetailPopupView_ViewMapRequested(object sender, PoiModel? poi)
    {
        if (poi == null || _tour == null) return;
        _ = Shell.Current.GoToAsync($"//map?TourId={Uri.EscapeDataString(_tour.Id)}&TourName={Uri.EscapeDataString(_tour.Name)}");
    }
}
