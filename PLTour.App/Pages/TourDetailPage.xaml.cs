using Microsoft.Maui.Controls;
using MauiColor = Microsoft.Maui.Graphics.Color;
using PLTour.App.Controls;
using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;

namespace PLTour.App.Pages;

[QueryProperty(nameof(Tour), nameof(Tour))]
[QueryProperty(nameof(Tour), "SelectedTour")]
public partial class TourDetailPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly IAudioService _audioService;
    private TourModel? _tour;
    private bool _isTracking;

    public TourModel? Tour
    {
        get => _tour;
        set
        {
            _tour = value;
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
        _locationService = locationService;
        _audioService = audioService;
        PoiList.ItemsSource = PoiListSource;
        SetActiveFilter(PoiCategories.ThamQuan);
        StartTracking();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isTracking = false;
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

    private async void StartTracking()
    {
        if (_isTracking) return;
        _isTracking = true;

        while (_isTracking)
        {
            try
            {
                var location = await _locationService.GetAndSaveCurrentLocationAsync();
                if (location != null)
                    UpdateDistances();
            }
            catch
            {
                // Bỏ qua lỗi định vị tạm thời để tránh crash UI
            }

            await Task.Delay(5000);
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
            poi.Address = distMeters < 1000 ? $"{Math.Round(distMeters)} m" : $"{(distMeters / 1000.0):F1} km";
        }

        ApplyCurrentFilter();
    }

    private void ApplyCurrentFilter()
    {
        if (_tour?.Pois == null) return;

        var activeCategory = GetActiveCategory();
        var filtered = _tour.Pois
            .Where(p => string.Equals(NormalizeCategory(p.Category), activeCategory, StringComparison.OrdinalIgnoreCase))
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
        this.PoiDetailPopup.ShowPopup(poi);
    }

    private async void SpeakIntro_Clicked(object? sender, EventArgs e)
    {
        if (_tour == null) return;

        var intro = _tour.IntroText?.Trim();
        if (string.IsNullOrWhiteSpace(intro)) return;

        await TextToSpeech.SpeakAsync(intro);
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

    private static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? PoiCategories.ThamQuan : category.Trim();

    private async void AudioPlayer_MediaEnded(object sender, EventArgs e)
    {
        await _audioService.StopAsync();

        if (_tour != null)
        {
            foreach (var poi in PoiListSource)
                poi.IsPlaying = false;
        }
    }

    private async void PoiDetailPopup_SpeakRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;
        await SpeakPoiAsync(poi);
    }

    private async Task SpeakPoiAsync(PoiModel poi)
    {
        try
        {
            foreach (var item in PoiListSource)
            {
                if (item != poi && item.IsPlaying)
                    item.IsPlaying = false;
            }

            poi.IsPlaying = true;
            await _audioService.StopAsync();

            var audioUrl = poi.AudioUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(audioUrl))
            {
                await _audioService.PlayAudioAsync(audioUrl);
                return;
            }

            var speakText = poi.FullContent;
            if (string.IsNullOrWhiteSpace(speakText))
                speakText = poi.Description;

            speakText = speakText?.Trim();
            if (string.IsNullOrWhiteSpace(speakText)) return;

            await TextToSpeech.SpeakAsync($"{poi.Name}. {speakText}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TOUR] Speak POI failed: {ex}");
        }
        finally
        {
            poi.IsPlaying = false;
        }
    }

    private async void PoiDetailPopup_CloseRequested(object sender, EventArgs e)
    {
        await _audioService.StopAsync();
        this.PoiDetailPopup.HidePopup();
    }

    private void PoiDetailPopup_ViewMapRequested(object sender, PoiModel? poi)
    {
        if (poi == null || _tour == null) return;
        _ = Shell.Current.GoToAsync($"//map?TourId={Uri.EscapeDataString(_tour.Id)}&TourName={Uri.EscapeDataString(_tour.Name)}");
    }
}
